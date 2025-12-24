using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastBiteGroupMCA.Persistentce.Configurations
{
    internal sealed class ContentReportsConfiguration : IEntityTypeConfiguration<ContentReport>
    {
        public void Configure(EntityTypeBuilder<ContentReport> builder)
        {
            builder.ToTable("ContentReports");
            builder.HasKey(x => x.ContentReportID);

            builder.Property(x => x.Reason)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(x => x.ReportedContentType)
                   .HasConversion<string>()  // Enum to string
                   .IsRequired();

            builder.Property(x => x.Status)
                   .HasConversion<string>() // Lưu Enum dưới dạng chuỗi trong DB
                   .IsRequired();

            // FK: User báo cáo
            builder.HasOne(x => x.ReportedByUser)
               .WithMany(u => u.ContentReports)
               .HasForeignKey(x => x.ReportedByUserID)
               .OnDelete(DeleteBehavior.Restrict);

            // FK: Nhóm chứa nội dung
            builder.HasOne(x => x.Group)
                .WithMany(g => g.ContentReports)
                .HasForeignKey(x => x.GroupID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
