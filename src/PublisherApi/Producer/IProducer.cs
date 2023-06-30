using Architecture.EventDriven.PublisherApi.Entities;

namespace Architecture.EventDriven.PublisherApi.Producer;

public interface IProducer
{
    void Publish(string integrationEvent, string @event);
    void Publish<TEvent>(string integrationEvent, TEvent @event);
}
