using FastBiteGroupMCA.Application.Notifications.Abstractions;
using FastBiteGroupMCA.Domain.Entities;
using FastBiteGroupMCA.Domain.Enum;

namespace FastBiteGroupMCA.Application.Notifications.Templates;

public record NewMessageNotificationEventData(Messages Message, Conversation Conversation);

public class NewMessageNotificationTemplate : INotificationTemplate<NewMessageNotificationEventData>
{
    public EnumNotificationType NotificationType => EnumNotificationType.NewMessage;

    public string BuildContent(NewMessageNotificationEventData data)
    {
        // Sử dụng lại helper GetMessagePreview để tạo nội dung tóm tắt
        var messagePreview = GetMessagePreview(data.Message);
        return $"Bạn có tin nhắn mới từ <strong>{data.Message.Sender?.DisplayName}</strong> trong <strong>{data.Conversation.Title}</strong>: \"{messagePreview}\"";
    }

    public RelatedObjectInfo BuildRelatedObject(NewMessageNotificationEventData data)
    {
        // Điều hướng người dùng đến cuộc trò chuyện
        string navigateUrl = data.Conversation.ConversationType == EnumConversationType.Group
            ? $"/groups/{data.Conversation.ExplicitGroupID}/chat"
            : $"/messages/{data.Conversation.ConversationID}";

        return new()
        {
            ObjectType = EnumNotificationObjectType.Message,
            ObjectId = data.Conversation.ConversationID.ToString(),
            NavigateUrl = navigateUrl
        };
    }
    private string GetMessagePreview(Messages message)
    {
        const int previewLength = 50;
        return message.MessageType switch
        {
            EnumMessageType.Image => "một hình ảnh",
            EnumMessageType.File => "một tệp đính kèm",
            EnumMessageType.Video => "một video",
            EnumMessageType.Audio => "một tin nhắn thoại",
            _ => message.Content.Length > previewLength
                   ? message.Content.Substring(0, previewLength) + "..."
                   : message.Content
        };
    }
}