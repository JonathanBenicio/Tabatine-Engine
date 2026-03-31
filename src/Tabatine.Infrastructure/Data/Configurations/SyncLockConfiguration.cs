using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations;

public class SyncLockConfiguration : IEntityTypeConfiguration<SyncLock>
{
    public void Configure(EntityTypeBuilder<SyncLock> builder)
    {
        builder.ToTable("sync_locks");

        builder.HasKey(e => e.LockKey);

        builder.Property(e => e.LockKey)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.LockToken)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();
    }
}
