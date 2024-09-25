using iThome2024.SalesService.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using iThome2024.SalesService.ViewModel;
using BC = BCrypt.Net.BCrypt;
using iThome2024.SalesService.Data;
using Microsoft.EntityFrameworkCore;
using iThome2024.SalesService.Data.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "SalesService API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddDbContext<TicketSalesContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TicketSalesContext"));
});
builder.Services.AddSingleton(new RedisService(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton(
    new PublisherService(builder.Configuration["GoogleCloud:ProjectId"] ?? "",
                         builder.Configuration["GoogleCloud:PubSub-Ticket:TopicId"] ?? ""));
builder.Services.AddSingleton<TokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthentication();
app.UseAuthorization();

#region Test API
app.MapPost("/Test/RedisAddString", async (RedisService redisService, string key, string value) =>
{
    await redisService.StringSetAsync(key, value);
    return $"Set {key} to {value}";
})
.WithName("TestRedisAddString")
.WithOpenApi();

app.MapGet("/Test/RedisGetString", async (string key, RedisService redisService) =>
{
    return await redisService.StringGetAsync(key);
})
.WithName("TestRedisGetString")
.WithOpenApi();

app.MapPost("/Test/PubSubPublishMessage", async (string message, [FromServices] PublisherService publisherService) =>
{
    return await publisherService.Publish(message);
})
.WithName("TestPubSubPublishMessage")
.WithOpenApi()
.RequireAuthorization();
#endregion

#region Auth API
// Sign Up endpoint
app.MapPost("/api/auth/user", async (UserSignInViewModel model, [FromServices] TicketSalesContext context) =>
{
    model.Username = model.Username.ToUpper();
    var user = await context.User.FirstOrDefaultAsync(u => u.Username == model.Username);
    if (user != null)
    {
        return Results.BadRequest("Username already exists");
    }
    user = new User
    {
        Username = model.Username,
        Password = BC.HashPassword(model.Password),
        CreateTime = DateTime.Now
    };
    await context.User.AddAsync(user);
    await context.SaveChangesAsync();

    return Results.Ok("User registered successfully");
});

// Sign In endpoint
app.MapPost("/api/auth", async (
    UserSignInViewModel model,
    [FromServices] TicketSalesContext context,
    [FromServices] TokenService tokenService) =>
{
    model.Username = model.Username.ToUpper();
    var user = await context.User.FirstOrDefaultAsync(u => u.Username == model.Username);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    if (!BC.Verify(model.Password, user.Password))
    {
        return Results.Unauthorized();
    }

    var token = tokenService.GenerateJwtToken(model.Username);
    return Results.Ok(new { token });
});
#endregion

#region Event API
app.MapGet("/api/event", async ([FromServices] TicketSalesContext context, [FromServices] RedisService redisService) =>
{
    return await context.Event.ToListAsync();
});

app.MapGet("/api/eventFromDb/{id}", async (int id, [FromServices] TicketSalesContext context, [FromServices] RedisService redisService) =>
{
    var entry = await context.Event.Include(e => e.Seats).FirstOrDefaultAsync(e => e.Id == id);
    if (entry == null)
    {
        return Results.NotFound();
    }
    return Results.Ok(entry);
});

app.MapGet("/api/event/{id}", async (int id, [FromServices] TicketSalesContext context, [FromServices] RedisService redisService) =>
{
    if (await redisService.KeyExistsAsync($"Event:{id}"))
    {
        var hash = await redisService.HashGetAllAsync($"Event:{id}");
        var redisEntry = new Event
        {
            Id = id,
            Name = hash["Name"],
            EventDate = DateTime.Parse(hash["EventDate"]),
            StartSalesDate = DateTime.Parse(hash["StartSalesDate"]),
            EndSalesDate = DateTime.Parse(hash["EndSalesDate"]),
            Description = hash["Description"],
            Remark = hash["Remark"],
            Seats = JsonSerializer.Deserialize<List<Seat>>(hash["Seats"]),
        };
        return Results.Ok(redisEntry);
    }
    else
    {
        var entry = await context.Event.Include(e => e.Seats).FirstOrDefaultAsync(e => e.Id == id);
        if (entry == null)
        {
            return Results.NotFound();
        }
        foreach (var property in entry.GetType().GetProperties())
        {
            if (property.Name != "Seats")
            { await redisService.HashSetAsync($"Event:{entry.Id}", property.Name, property.GetValue(entry)!.ToString()!); }
        }
        await redisService.HashSetAsync($"Event:{entry.Id}", "Seats", JsonSerializer.Serialize(entry.Seats));
        return Results.Ok(entry);
    }
});

app.MapPost("/api/event", async (EventCreateViewModel model, [FromServices] TicketSalesContext context, [FromServices] RedisService redisService) =>
{
    // Map EventCreateViewModel to Event
    var newEvent = new Event
    {
        Name = model.Name,
        EventDate = model.EventDate,
        StartSalesDate = model.StartSalesDate,
        EndSalesDate = model.EndSalesDate,
        Description = model.Description,
        Remark = model.Remark,
    };

    // 新增 Event
    await context.Event.AddAsync(newEvent);

    // 新增相關的 Seat
    if (model.Seats != null && model.Seats.Any())
    {
        foreach (var seat in model.Seats)
        {
            var newSeat = new Seat
            {
                Name = seat.Name,
                Area = seat.Area,
                Status = seat.Status,
                Event = newEvent
            };
            await context.Seat.AddAsync(newSeat);
        }
    }
    await context.SaveChangesAsync();

    return Results.Created($"/api/event/{newEvent.Id}", newEvent);
});

app.MapPut("/api/event/{id}", async (int id, EventCreateViewModel model, [FromServices] TicketSalesContext context, [FromServices] RedisService redisService) =>
{
    var entity = await context.Event.Include(e => e.Seats).FirstOrDefaultAsync(e => e.Id == id);
    if (entity == null)
    {
        return Results.NotFound();
    }

    // 更新 Event 的屬性
    entity.Name = model.Name;
    entity.EventDate = model.EventDate;
    entity.StartSalesDate = model.StartSalesDate;
    entity.EndSalesDate = model.EndSalesDate;
    entity.Description = model.Description;
    entity.Remark = model.Remark;

    // 更新 Seats
    if (model.Seats != null)
    {
        // 刪除現有的 Seats
        context.Seat.RemoveRange(entity.Seats);

        // 新增傳入的 Seats
        foreach (var seat in model.Seats)
        {
            seat.EventId = entity.Id; // 設定 Seat 的 EventId
            var newSeat = new Seat
            {
                Name = seat.Name,
                Area = seat.Area,
                Status = seat.Status,
                EventId = entity.Id
            };
            await context.Seat.AddAsync(newSeat);
        }
    }
    await context.SaveChangesAsync();
    await redisService.KeyDeleteAsync($"Event:{id}");
    return Results.Ok(entity);
});

app.MapDelete("/api/event/{id}", async (int id, [FromServices] TicketSalesContext context) =>
{
    var entity = await context.Event.FindAsync(id);
    if (entity == null)
    {
        return Results.NotFound();
    }
    // 刪除相關的 Seats
    context.Seat.RemoveRange(entity.Seats);

    // 刪除 Event
    context.Event.Remove(entity);
    await context.SaveChangesAsync();
    return Results.NoContent();
});
#endregion
app.Run();