using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations;

public class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.AppKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Event)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.MessageId)
            .HasMaxLength(100);

        builder.Property(e => e.LastErrorDetail)
            .HasColumnType("text");

        builder.Property(e => e.RetryCount)
            .HasDefaultValue(0);

        builder.Property(e => e.MaxRetries)
            .HasDefaultValue(5);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");

        // Índices para performance na fila e no dashboard admin
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.NextRetryAt)
            .HasFilter("\"status\" = 'Failed'");
        builder.HasIndex(e => e.Event);
        builder.HasIndex(e => e.MessageId);
    }
}
