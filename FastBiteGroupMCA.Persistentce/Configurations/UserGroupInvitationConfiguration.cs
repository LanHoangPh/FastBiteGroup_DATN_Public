
namespace FastBiteGroupMCA.Persistentce.Configurations;

internal class UserGroupInvitationConfiguration : IEntityTypeConfiguration<UserGroupInvitation>
{
    public void Configure(EntityTypeBuilder<UserGroupInvitation> builder)
    {
        builder.ToTable(TableNames.UserGroupInvitations);
        builder.HasKey(x => x.InvitationID);

        // Ràng buộc unique: Một user chỉ có thể nhận một lời mời từ một nhóm cụ thể
        builder.HasIndex(x => new { x.InvitedUserID, x.GroupID })
               .IsUnique()
               .HasDatabaseName("IX_Unique_User_Group_Invitation");
        // Cấu hình các thuộc tính
        builder.Property(x => x.InvitationID)
               .ValueGeneratedOnAdd();
        builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>(); // Chuyển đổi Enum sang string trong CSDL
        // Cấu hình mối quan hệ với User và Group
        builder.HasOne(invitation => invitation.Group)
               .WithMany(group => group.DirectUserInvitations) 
               .HasForeignKey(invitation => invitation.GroupID)
               .OnDelete(DeleteBehavior.Cascade); // Nếu xóa Group, các lời mời liên quan cũng sẽ bị xóa

        // 2. Mối quan hệ với AppUser với vai trò "Người được mời"
        builder.HasOne(invitation => invitation.InvitedUser)
               .WithMany(user => user.ReceivedInvitations) // Trỏ đến collection các lời mời user "nhận được"
               .HasForeignKey(invitation => invitation.InvitedUserID)
               .OnDelete(DeleteBehavior.Restrict); // Ngăn việc xóa một user nếu họ có lời mời đang chờ

        // 3. Mối quan hệ với AppUser với vai trò "Người mời"
        builder.HasOne(invitation => invitation.InvitedByUser)
               .WithMany(user => user.SentInvitations) // Trỏ đến collection các lời mời user "đã gửi"
               .HasForeignKey(invitation => invitation.InvitedByUserID)
               .OnDelete(DeleteBehavior.Restrict); // Ngăn việc xóa một user nếu họ đã gửi lời mời

    }
}
