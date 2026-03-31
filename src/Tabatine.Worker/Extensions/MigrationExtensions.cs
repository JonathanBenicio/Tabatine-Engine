using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tabatine.Infrastructure.Data;
using Serilog;

namespace Tabatine.Worker.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            Log.Information("Aplicando migrações de banco de dados...");
            
            // Usar o Migrator diretamente para evitar o "Database.Exists" check interno que quebra no Pooler do Supabase
            var migrator = db.GetService<IMigrator>();
            migrator.Migrate();

            Log.Information("Migrações aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ocorreu um erro ao aplicar as migrações do banco de dados.");
            throw;
        }
    }
}
