using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Architecture.EventDriven.ConsumerApi.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Channels;
using Architecture.EventDriven.ConsumerApi.Entities;
using Architecture.EventDriven.ConsumerApi.Services;

namespace Architecture.EventDriven.ConsumerApi.HostedServices;

public class UserConsumer : IHostedService
{
    private IModel? _channel = null;
    private IConnection? _connection = null;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RabbitMqConfiguration> _config;

    public UserConsumer(IServiceScopeFactory scopeFactory, IOptions<RabbitMqConfiguration> config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }

    // Initiate RabbitMQ and start listening to an input queue
    private void Run()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config.Value.Hostname,
            Port = _config.Value.Port,
            UserName = _config.Value.UserName,
            Password = _config.Value.Password,
            DispatchConsumersAsync = true
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        Console.WriteLine(" [*] Waiting for messages.");

        _channel.ExchangeDeclare(exchange: "userExchange", ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(queue: "user", durable: false, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: "user", exchange: "userExchange", routingKey: "user.*");
        
        //_channel.BasicQos(0, 10, false);

        var consumer = new AsyncEventingBasicConsumer(this._channel);
        consumer.Received += OnMessageRecieved;

        this._channel.BasicConsume(queue: "user", autoAck: false, consumer: consumer);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this.Run();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        this._channel?.Dispose();
        this._connection?.Dispose();
        return Task.CompletedTask;
    }

    // Publish a received  message with "reply:" prefix
    private Task OnMessageRecieved(object? model, BasicDeliverEventArgs @event)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PostServiceContext>();

        var body = @event.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine(" [x] Received {0}", message);

        var data = JsonSerializer.Deserialize<User>(message);
        var type = @event.RoutingKey;
        Console.WriteLine($"RoutingKey: {type}");

        //if (data == null) 
        //    return Task.CompletedTask;

        switch (type)
        {
            case "user.add":
                dbContext.User.Add(data);
                dbContext.SaveChanges();
                break;
            case "user.update":
            {
                var user = dbContext.User.First(a => a.Id == data!.Id);
                user.Name = data?.Name;
                dbContext.SaveChanges();
                break;
            }
        }
        Console.WriteLine(" [x] Done");
        this._channel?.BasicAck(deliveryTag: @event.DeliveryTag, multiple: false);
        return Task.CompletedTask;
    }
}