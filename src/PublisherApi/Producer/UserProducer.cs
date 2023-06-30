using RabbitMQ.Client;
using System.Text.Json;
using System.Text;
using Architecture.EventDriven.PublisherApi.Configuration;
using Microsoft.Extensions.Options;

namespace Architecture.EventDriven.PublisherApi.Producer;

public class UserProducer : IProducer
{
    private IModel? _channel = null;
    private IConnection? _connection = null;
    private readonly IOptions<RabbitMqConfiguration> _config;

    public UserProducer(IOptions<RabbitMqConfiguration> config)
    {
        _config = config;
    }

    public void Publish(string integrationEvent, string @event)
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

        var body = Encoding.UTF8.GetBytes(@event);
        var properties = _channel.CreateBasicProperties();
        properties.ContentType = "application/json";
        properties.DeliveryMode = 1; // Doesn't persist to disk
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.ExchangeDeclare(exchange: "userExchange", ExchangeType.Direct, durable: true, autoDelete: false);
        _channel.QueueDeclare(queue: "user", durable: false, exclusive: false, autoDelete: false);
        _channel.QueueBind(queue: "user", exchange: "userExchange", routingKey: integrationEvent);
        _channel.BasicPublish("userExchange", integrationEvent, properties, body);
    }

    public void Publish<TEvent>(string integrationEvent, TEvent @event)
    {
        var body = JsonSerializer.Serialize(@event);
        Publish(integrationEvent, body);
    }
}