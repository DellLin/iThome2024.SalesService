using iThome2024.SalesService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TicketSalesContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TicketSalesContext"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/TestDbConnection", (TicketSalesContext context) =>
{
    return context.Database.CanConnect();
})
.WithName("TestDbConnection")
.WithOpenApi(); ;

app.Run();