namespace FastBiteGroupMCA.Persistentce.Configurations
{
    internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.ToTable(TableNames.Groups);
            builder.HasKey(x => x.GroupID);

            builder.Property(x => x.GroupName).IsRequired().HasMaxLength(255);
            builder.Property(x => x.Description).HasMaxLength(1000);

            // Cấu hình Enum
            builder.Property(x => x.GroupType)
                   .IsRequired()
                   .HasConversion<string>()
                   .HasMaxLength(50);
            builder.Property(x => x.Privacy)
                     .IsRequired()
                     .HasConversion<string>()
                     .HasMaxLength(50);

            // Mối quan hệ: Một User có thể tạo nhiều Group
            builder.HasOne(g => g.CreatedByUser)
                   .WithMany(u => u.CreatedGroups)
                   .HasForeignKey(g => g.CreatedByUserID)
                   .OnDelete(DeleteBehavior.Restrict);

            // Cấu hình query filter cho Soft Delete
            builder.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
