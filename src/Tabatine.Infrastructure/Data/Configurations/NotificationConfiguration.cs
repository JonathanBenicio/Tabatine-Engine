using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications", "public");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).HasColumnName("Id").ValueGeneratedOnAdd();
            builder.Property(e => e.Title).HasColumnName("title").IsRequired();
            builder.Property(e => e.Message).HasColumnName("message").IsRequired();
            builder.Property(e => e.Type).HasColumnName("type").IsRequired();
            builder.Property(e => e.ReferenceId).HasColumnName("reference_id");
            builder.Property(e => e.IsRead).HasColumnName("is_read").HasDefaultValue(false);
            builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        }
    }
}
