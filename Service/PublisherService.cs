using Google.Cloud.PubSub.V1;

namespace iThome2024.SalesService.Service;

public class PublisherService
{
    private PublisherClient _publisher;

    public PublisherService(string projectId, string topicId)
    {
        TopicName topicName = new(projectId, topicId);
        _publisher = PublisherClient.Create(topicName);
    }
    public async Task<string> Publish(string message)
    {
        return await _publisher.PublishAsync(message);
    }
}
