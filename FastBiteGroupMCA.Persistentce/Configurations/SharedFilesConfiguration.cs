namespace FastBiteGroupMCA.Persistentce.Configurations;

internal sealed class SharedFilesConfiguration : IEntityTypeConfiguration<SharedFiles>
{
    public void Configure(EntityTypeBuilder<SharedFiles> builder)
    {
        builder.ToTable(TableNames.SharedFiles);
        builder.HasKey(x => x.FileID);

        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.StorageUrl).IsRequired().HasMaxLength(2048);
        builder.Property(x => x.FileType).HasMaxLength(100);

        // Cấu hình query filter cho Soft Delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Mối quan hệ với User tải file lên
        builder.HasOne(sf => sf.UploadedByUser)
               .WithMany(u => u.UploadedFiles)
               .HasForeignKey(sf => sf.UploadedByUserID)
               .OnDelete(DeleteBehavior.Restrict);

        // Mối quan hệ với Conversation

        // Mối quan hệ tùy chọn với Message
        //builder.HasOne<Messages>()
        //       .WithMany()
        //       .HasForeignKey(sf => sf.MessageID)
        //       .IsRequired(false);
    }
}

