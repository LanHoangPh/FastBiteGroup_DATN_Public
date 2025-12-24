namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class PostCommentsConfiguration : IEntityTypeConfiguration<PostComments>
{
    public void Configure(EntityTypeBuilder<PostComments> builder)
    {
        builder.ToTable(TableNames.PostComments);
        builder.HasKey(x => x.CommentID);

        builder.Property(x => x.Content).IsRequired().HasMaxLength(2000);

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Mối quan hệ với Post
        builder.HasOne(pc => pc.Post)
               .WithMany(p => p.Comments)
               .HasForeignKey(pc => pc.PostID)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User tạo comment
        builder.HasOne(pc => pc.User)
               .WithMany(u => u.PostComments)
               .HasForeignKey(pc => pc.UserID)
               .OnDelete(DeleteBehavior.Restrict);

        // Mối quan hệ tự tham chiếu cho việc trả lời comment
        builder.HasOne(pc => pc.ParentComment)
               .WithMany(p => p.Replies)
               .HasForeignKey(pc => pc.ParentCommentID)
               .IsRequired(false);
    }
}
