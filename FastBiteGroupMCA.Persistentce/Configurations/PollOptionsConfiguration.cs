namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class PollOptionsConfiguration : IEntityTypeConfiguration<PollOptions>
{
    public void Configure(EntityTypeBuilder<PollOptions> builder)
    {
        builder.ToTable(TableNames.PollOptions);
        builder.HasKey(x => x.PollOptionID);

        builder.Property(x => x.OptionText).IsRequired().HasMaxLength(500);

        // Mối quan hệ với Poll
        builder.HasOne(po => po.Poll)
               .WithMany(p => p.Options)
               .HasForeignKey(po => po.PollID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
