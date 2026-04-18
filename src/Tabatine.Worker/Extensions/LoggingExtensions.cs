using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL.ColumnWriters;

namespace Tabatine.Worker.Extensions;

public static class LoggingExtensions
{
    public static void AddCustomLogging(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection not found");

        Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"[SERILOG SELF-LOG] {msg}"));

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Polly", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                restrictedToMinimumLevel: LogEventLevel.Debug,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug);

        // Desativa o sink do PostgreSQL durante os testes de integração para evitar ruído e erros de dispose dos containers
        if (builder.Environment.EnvironmentName != "Testing")
        {
            loggerConfig.WriteTo.PostgreSQL(
                connectionString: connectionString,
                tableName: "logs",
                schemaName: "",
                batchSizeLimit: 1,
                needAutoCreateTable: false,
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: new Dictionary<string, ColumnWriterBase>
                {
                    { "message", new RenderedMessageColumnWriter() },
                    { "message_template", new MessageTemplateColumnWriter() },
                    { "level", new LevelColumnWriter(true, NpgsqlTypes.NpgsqlDbType.Text) },
                    { "timestamp", new TimestampColumnWriter() },
                    { "exception", new ExceptionColumnWriter() },
                    { "properties", new PropertiesColumnWriter() },
                    { "log_event", new LogEventSerializedColumnWriter() }
                });
        }

        Log.Logger = loggerConfig.CreateLogger();

        builder.Host.UseSerilog();
    }
}
