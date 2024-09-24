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
app.MapPost("/Test/RedisAddString", (RedisService redisService, string key, string value) =>
{
    redisService.StringSet(key, value);
    return $"Set {key} to {value}";
})
.WithName("TestRedisAddString")
.WithOpenApi();

app.MapGet("/Test/RedisGetString", (string key, RedisService redisService) =>
{
    return redisService.StringGet(key).ToString();
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
app.Run();