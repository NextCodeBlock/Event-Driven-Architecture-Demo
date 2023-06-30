using System.Text;
using System.Text.Json;
using Architecture.EventDriven.ConsumerApi.Configuration;
using Architecture.EventDriven.ConsumerApi.Entities;
using Architecture.EventDriven.ConsumerApi.HostedServices;
using Architecture.EventDriven.ConsumerApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);
IConfiguration config = builder.Configuration;

// Add services to the container.
builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rabbitMqConfiguration = config.GetSection(nameof(RabbitMqConfiguration));
builder.Services.Configure<RabbitMqConfiguration>(rabbitMqConfiguration);
builder.Services.AddHostedService<UserConsumer>();

builder.Services.AddDbContext<PostServiceContext>(options =>
    options.UseSqlite(builder.Configuration["ConnectionStrings:Default"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PostServiceContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/users", async (PostServiceContext db) =>
    await db.User.ToListAsync());

app.MapGet("/posts", async (PostServiceContext db) =>
    await db.Post.Include(x=>x.User).ToListAsync());

app.MapPost("/add", async (Post post, PostServiceContext db) =>
{
    db.Post.Add(post);
    await db.SaveChangesAsync();

    return Results.Created($"/save/{post.PostId}", post);
});
app.Run();