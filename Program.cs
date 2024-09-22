using iThome2024.SalesService.Data;
using iThome2024.SalesService.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TicketSalesContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("TicketSalesContext"));
});
builder.Services.AddSingleton<RedisService>(new RedisService(builder.Configuration.GetConnectionString("Redis")!));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



app.MapGet("/Test/DbConnection", (TicketSalesContext context) =>
{
    return context.Database.CanConnect();
})
.WithName("TestDbConnection")
.WithOpenApi();

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

app.MapPost("/Test/PubSubPublishMessage", async (string message, PublisherService publisherService) =>
{
    return await publisherService.Publish(message);
})
.WithName("TestPubSubPublishMessage")
.WithOpenApi();

app.MapGet("/Test/PubSubSubscribeMessage", async (SubscriberService subscriberService) =>
{
    return await subscriberService.Subscribe();
})
.WithName("TestPubSubSubscribeMessage")
.WithOpenApi();

app.Run();