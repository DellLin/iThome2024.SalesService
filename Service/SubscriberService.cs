using Google.Cloud.PubSub.V1;

namespace iThome2024.SalesService.Service;

public class SubscriberService
{

    private SubscriptionName _subscriptionName;
    public SubscriberService(string projectId, string subscriptionId)
    {
        _subscriptionName = new(projectId, subscriptionId);

    }
    public async Task<List<string>> Subscribe()
    {
        var subscriber = await SubscriberClient.CreateAsync(_subscriptionName);
        List<string> receivedMessages = [];
        await subscriber.StartAsync((msg, cancellationToken) =>
        {
            receivedMessages.Add(msg.Data.ToStringUtf8());
            subscriber.StopAsync(TimeSpan.FromSeconds(15));
            return Task.FromResult(SubscriberClient.Reply.Ack);
        });
        return receivedMessages;
    }

}
