namespace FastBiteGroupMCA.Infastructure.Messaging;

public interface IPubSubService
{
    Task PublishSettingsUpdateAsync();
    void SubscribeToSettingsUpdates(Action onMessageReceived);
}
