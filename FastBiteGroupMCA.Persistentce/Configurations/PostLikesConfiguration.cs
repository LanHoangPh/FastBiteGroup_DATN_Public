using FastBiteGroupMCA.Persistentce.Constants;

namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class PostLikesConfiguration : IEntityTypeConfiguration<PostLikes>
{
    public void Configure(EntityTypeBuilder<PostLikes> builder)
    {
        builder.ToTable(TableNames.PostLikes);
        builder.HasKey(x => x.LikeID);

        // Mỗi user chỉ được like 1 post 1 lần
        builder.HasIndex(x => new { x.PostID, x.UserID }).IsUnique();

        builder.HasOne(pl => pl.Post)
               .WithMany(p => p.Likes)
               .HasForeignKey(pl => pl.PostID)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pl => pl.User)
               .WithMany(u => u.PostLikes)
               .HasForeignKey(pl => pl.UserID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
