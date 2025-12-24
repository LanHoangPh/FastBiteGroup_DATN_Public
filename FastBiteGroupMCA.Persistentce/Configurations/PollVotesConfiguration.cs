namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class PollVotesConfiguration : IEntityTypeConfiguration<PollVotes>
{
    public void Configure(EntityTypeBuilder<PollVotes> builder)
    {
        builder.ToTable(TableNames.PollVotes);
        builder.HasKey(x => x.PollVoteID);

        // Ràng buộc unique: Một user chỉ vote 1 lần cho 1 option
        builder.HasIndex(x => new { x.PollOptionID, x.UserID }).IsUnique();

        // Mối quan hệ với PollOption
        builder.HasOne(pv => pv.PollOption)
               .WithMany(po => po.Votes)
               .HasForeignKey(pv => pv.PollOptionID)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User
        builder.HasOne(pv => pv.User)
               .WithMany(u => u.PollVotes)
               .HasForeignKey(pv => pv.UserID)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
