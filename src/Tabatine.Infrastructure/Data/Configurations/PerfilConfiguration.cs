using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabatine.Core.Entities;

namespace Tabatine.Infrastructure.Data.Configurations;

public class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("perfis");
        
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Nome).HasMaxLength(150);

        builder.HasIndex(p => p.TelegramChatId).IsUnique();
        builder.HasIndex(p => p.TelegramLinkToken).IsUnique();

        builder.Property(p => p.ReceiveLogs)
               .HasDefaultValue(false);

        builder.Property(p => p.CreatedAt)
               .HasDefaultValueSql("now()");

        builder.Property(p => p.UpdatedAt)
               .ValueGeneratedOnAddOrUpdate();
    }
}
