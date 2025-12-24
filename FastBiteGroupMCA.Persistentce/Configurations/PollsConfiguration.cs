namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class PollsConfiguration : IEntityTypeConfiguration<Polls>
{
    public void Configure(EntityTypeBuilder<Polls> builder)
    {
        builder.ToTable(TableNames.Polls);
        builder.HasKey(x => x.PollID);

        builder.Property(x => x.Question).IsRequired().HasMaxLength(1000);

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Mối quan hệ với Conversation
        builder.HasOne(p => p.Conversation)
               .WithMany()
               .HasForeignKey(p => p.ConversationID)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User tạo poll
        builder.HasOne(p => p.CreatedByUser)
               .WithMany(u => u.CreatedPolls)
               .HasForeignKey(p => p.CreatedByUserID)
               .OnDelete(DeleteBehavior.Restrict);

        // Mối quan hệ tùy chọn với Message
        //builder.HasOne<Messages>()
        //       .WithOne()
        //       .HasForeignKey<Polls>(p => p.MessageID)
        //       .IsRequired(false);
    }
}
