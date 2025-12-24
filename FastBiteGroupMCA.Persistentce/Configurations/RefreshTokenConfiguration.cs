namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(TableNames.RefreshTokens); 
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).IsRequired().HasMaxLength(255);
        builder.HasIndex(x => x.Token).IsUnique();
        // Mối quan hệ với User
        builder.HasOne(rt => rt.User)
               .WithMany() // Một User có thể có nhiều Refresh Token
               .HasForeignKey(rt => rt.UserId)
               .OnDelete(DeleteBehavior.Cascade); // Xóa hết token nếu User bị xóa
    }
}
