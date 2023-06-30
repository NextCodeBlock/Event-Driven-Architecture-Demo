using Architecture.EventDriven.PublisherApi;
using Microsoft.EntityFrameworkCore;
using Architecture.EventDriven.PublisherApi.Configuration;
using Architecture.EventDriven.PublisherApi.Producer;
using Architecture.EventDriven.PublisherApi.Entities;
using Architecture.EventDriven.PublisherApi.Services;

var builder = WebApplication.CreateBuilder(args);
IConfiguration config = builder.Configuration;

// Add services to the container.
builder.Services.AddAuthorization();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var rabbitMqConfiguration = config.GetSection(nameof(RabbitMqConfiguration));
builder.Services.Configure<RabbitMqConfiguration>(rabbitMqConfiguration);
builder.Services.AddSingleton<IProducer, UserProducer>();

builder.Services.AddDbContext<UserServiceContext>(options =>
    options.UseSqlite(builder.Configuration["ConnectionStrings:Default"]));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapGet("/get", async (UserServiceContext db) =>
    await db.User.ToListAsync());

app.MapPost("/add", async (User user, UserServiceContext db, IProducer producer) =>
{
    db.User.Add(user);
    await db.SaveChangesAsync();

    var @event = new
    {
        Id = user.Id,
        Name = user.Name
    };
    producer.Publish("user.add", @event);

    return Results.Created($"/add/{user.Id}", user);
});

app.MapPut("/update/{id}", async (int id, User userInput, UserServiceContext db, IProducer producer) =>
{
    var user = await db.User.FindAsync(id);

    if (user is null) return Results.NotFound();

    user.Name = userInput.Name;
    await db.SaveChangesAsync();

    var @event = new
    {
        Id = user.Id,
        Name = user.Name
    };
    producer.Publish("user.update", @event);

    return Results.NoContent();
});
app.Run();