using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Catalog.Api.Repositories;
using Catalog.Api.Settings;
using System.Text.Json;
using System.Net.Mime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
BsonSerializer.RegisterSerializer(new GuidSerializer(BsonType.String));
BsonSerializer.RegisterSerializer(new DateTimeOffsetSerializer(BsonType.String));

var mongoDbSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
builder.Services.AddSingleton<IMongoClient>(ServiceProvider =>
{
  return new MongoClient(mongoDbSettings.ConnectionString);
});

builder.Services.AddSingleton<IItemsRepository, MongoDbItemsRepository>();
builder.Services.AddControllers(options =>
{
  options.SuppressAsyncSuffixInActionNames = false;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHealthChecks().AddMongoDb(mongoDbSettings.ConnectionString,
name: "mongodb",
timeout: TimeSpan.FromSeconds(3),
tags: new[] { "ready" });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
  app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  Predicate = check => check.Tags.Contains("ready"),
  ResponseWriter = async (context, report) =>
  {
    var result = JsonSerializer.Serialize(new
    {
      status = report.Status.ToString(),
      checks = report.Entries.Select(entry => new
      {
        name = entry.Key,
        status = entry.Value.Status.ToString(),
        exception = entry.Value.Exception != null ? entry.Value.Exception.Message : "none",
        duration = entry.Value.Duration.ToString()
      })
    });

    context.Response.ContentType = MediaTypeNames.Application.Json;
    await context.Response.WriteAsync(result);
  }
});

app.MapHealthChecks("health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
  Predicate = _ => false
});

app.MapControllers();

app.Run();
