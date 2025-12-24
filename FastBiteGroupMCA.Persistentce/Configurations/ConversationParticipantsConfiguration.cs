namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class ConversationParticipantsConfiguration : IEntityTypeConfiguration<ConversationParticipants>
{
    public void Configure(EntityTypeBuilder<ConversationParticipants> builder)
    {
        builder.ToTable(TableNames.ConversationParticipants);
        builder.HasKey(x => x.ConversationParticipantID);

        // Ràng buộc unique
        builder.HasIndex(x => new { x.ConversationID, x.UserID }).IsUnique();

        // Mối quan hệ với Conversation
        builder.HasOne(cp => cp.Conversation)
               .WithMany(c => c.Participants)
               .HasForeignKey(cp => cp.ConversationID)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User
        builder.HasOne(cp => cp.User)
               .WithMany(u => u.ConversationParticipations)
               .HasForeignKey(cp => cp.UserID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
