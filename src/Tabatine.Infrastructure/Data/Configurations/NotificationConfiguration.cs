using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("notifications");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Title).IsRequired();
            builder.Property(e => e.Message).IsRequired();
            builder.Property(e => e.Type).IsRequired();
            builder.Property(e => e.IsRead).HasDefaultValue(false);
            builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        }
    }
}
