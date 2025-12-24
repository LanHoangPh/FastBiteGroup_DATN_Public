namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        // set tên bảng
        builder.ToTable(TableNames.Users);
        // cấu hình khóa chính
        builder.HasKey(user => user.Id);

        builder.Property(x =>x.FisrtName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.AvatarUrl).HasMaxLength(2048);
        builder.Property(x => x.Bio).HasMaxLength(500);
        builder.Property(x => x.OneSignalPlayerId).HasMaxLength(50);

        // Cấu hình thuộc tính Enum
        builder.Property(x => x.PresenceStatus)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);
        builder.Property(x => x.MessagingPrivacy)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);

    }
}
