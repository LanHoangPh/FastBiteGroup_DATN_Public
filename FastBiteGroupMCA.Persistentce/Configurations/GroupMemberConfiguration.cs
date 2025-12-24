using FastBiteGroupMCA.Persistentce.Constants;

namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable(TableNames.GroupMembers);
        builder.HasKey(x => x.GroupMemberID);

        // Ràng buộc unique: Một user chỉ tham gia 1 group 1 lần
        builder.HasIndex(x => new { x.GroupID, x.UserID }).IsUnique();

        // Cấu hình Enum
        builder.Property(x => x.Role)
               .IsRequired()
               .HasConversion<string>()
               .HasMaxLength(50);

        // Mối quan hệ với Group
        builder.HasOne(gm => gm.Group)
               .WithMany(g => g.Members)
               .HasForeignKey(gm => gm.GroupID)
               .OnDelete(DeleteBehavior.Cascade);

        // Mối quan hệ với User
        builder.HasOne(gm => gm.User)
               .WithMany(u => u.GroupMemberships)
               .HasForeignKey(gm => gm.UserID)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
