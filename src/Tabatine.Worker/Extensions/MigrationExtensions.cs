using Microsoft.EntityFrameworkCore;
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
            db.Database.Migrate();
            Log.Information("Migrações aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ocorreu um erro ao aplicar as migrações do banco de dados.");
            throw;
        }
    }
}
