namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class ConversationsConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable(TableNames.Conversations);
        builder.HasKey(x => x.ConversationID);

        builder.Property(x => x.Title).HasMaxLength(255);
        builder.Property(x => x.AvatarUrl).HasMaxLength(2048);

        // Đảm bảo rằng một GroupID chỉ có thể xuất hiện một lần trong bảng Conversation
        builder.HasIndex(x => x.ExplicitGroupID)
               .IsUnique()
               .HasFilter("[ExplicitGroupID] IS NOT NULL"); // Áp dụng cho các bản ghi không null

        // Cấu hình Enum
        builder.Property(x => x.ConversationType)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // Mối quan hệ tùy chọn với Group
        builder.HasOne(c => c.Group)
               .WithOne()
               .HasForeignKey<Conversation>(c => c.ExplicitGroupID)
               .IsRequired(false);

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
