namespace FastBiteGroupMCA.Application.DTOs.VideoCall;

public class CallHistoryItemDTO
{
    public Guid VideoCallSessionId { get; set; }
    public Guid InitiatorUserId { get; set; }
    /// <example>Nguyễn Văn A</example>
    public string InitiatorName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    /// <summary>Thời lượng cuộc gọi (tính bằng phút).</summary>
    public double DurationInMinutes => EndedAt.HasValue ? (EndedAt.Value - StartedAt).TotalMinutes : 0;
    /// <summary>Số người đã tham gia cuộc gọi.</summary>
    public int ParticipantCount { get; set; }
}
