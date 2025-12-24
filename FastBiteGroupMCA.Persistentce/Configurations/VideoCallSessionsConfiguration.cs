using FastBiteGroupMCA.Persistentce.Constants;

namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class VideoCallSessionsConfiguration : IEntityTypeConfiguration<VideoCallSessions>
{
    public void Configure(EntityTypeBuilder<VideoCallSessions> builder)
    {
        builder.ToTable(TableNames.VideoCallSessions);

        builder.HasKey(x => x.VideoCallSessionID);
        builder.Property(x => x.VideoCallSessionID).HasDefaultValueSql("NEWID()");

        builder.Property(x => x.Title).HasMaxLength(255);

        builder.Property(x => x.Status)
                   .HasConversion<string>() // Lưu Enum dưới dạng chuỗi trong DB
                   .IsRequired();
        builder.Property(x => x.TimeoutJobId).HasMaxLength(100);
        builder.Property(x => x.StartedAt).IsRequired();

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Mối quan hệ tùy chọn với Conversation
        builder.HasOne(v => v.Conversation)
               .WithMany()
               .HasForeignKey(v => v.ConversationID)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User khởi tạo
        builder.HasOne(v => v.UserInitiator)
               .WithMany(u => u.InitiatedVideoCallSessions)
               .HasForeignKey(v => v.InitiatorUserID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
