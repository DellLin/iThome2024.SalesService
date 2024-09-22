using iThome2024.SalesService.Service;
using Microsoft.AspNetCore.Mvc;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(new RedisService(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton(
    new PublisherService(builder.Configuration["GoogleCloud:ProjectId"] ?? "",
                         builder.Configuration["GoogleCloud:PubSub-Ticket:TopicId"] ?? ""));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
.WithOpenApi();

app.Run();