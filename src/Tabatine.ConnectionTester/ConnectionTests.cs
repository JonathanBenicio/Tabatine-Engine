using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace Tabatine.ConnectionTester;

public class DatabaseFixture : IAsyncLifetime
{
    private NpgsqlConnection? _connection;
    public NpgsqlConnection Connection => _connection!;

    public async Task InitializeAsync()
    {
        DotNetEnv.Env.Load(".env.local");
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        
        Assert.False(string.IsNullOrEmpty(connectionString), 
            "ConnectionStrings__DefaultConnection not found in .env.local");
        
        _connection = new NpgsqlConnection(connectionString);
        await _connection.OpenAsync();
        
        await CreateTestTable();
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            try { await DropTestTable(); } catch { }
            await _connection.CloseAsync();
            await _connection.DisposeAsync();
        }
    }

    private async Task CreateTestTable()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS integration_test_items (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100) NOT NULL,
                description TEXT,
                created_at TIMESTAMP DEFAULT NOW()
            )
            """;
        
        await using var cmd = new NpgsqlCommand(sql, _connection);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task DropTestTable()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open) return;
        
        try
        {
            await using var cmd = new NpgsqlCommand("DROP TABLE IF EXISTS integration_test_items", _connection);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}

[Collection("Database")]
public class ConnectionTests
{
    private readonly ITestOutputHelper _output;
    private readonly DatabaseFixture _fixture;

    public ConnectionTests(DatabaseFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public void Connection_ShouldConnect()
    {
        Assert.NotNull(_fixture.Connection);
        Assert.Equal(System.Data.ConnectionState.Open, _fixture.Connection.State);
        _output.WriteLine("Connection successful!");
    }

    [Fact]
    public async Task Insert_ShouldSucceed()
    {
        Assert.NotNull(_fixture.Connection);
        
        const string sql = """
            INSERT INTO integration_test_items (name, description)
            VALUES (@name, @description)
            RETURNING id
            """;
        
        await using var cmd = new NpgsqlCommand(sql, _fixture.Connection);
        cmd.Parameters.AddWithValue("name", "Test Item");
        cmd.Parameters.AddWithValue("description", "A test item for integration testing");
        
        var insertedId = await cmd.ExecuteScalarAsync();
        
        Assert.NotNull(insertedId);
        _output.WriteLine($"Inserted item with ID: {insertedId}");
    }

    [Fact]
    public async Task Select_ShouldReturnData()
    {
        Assert.NotNull(_fixture.Connection);
        
        await using var insertCmd = new NpgsqlCommand("""
            INSERT INTO integration_test_items (name, description)
            VALUES (@name, @description)
            RETURNING id
            """, _fixture.Connection);
        insertCmd.Parameters.AddWithValue("name", "Select Test");
        insertCmd.Parameters.AddWithValue("description", "Testing SELECT");
        var insertedId = await insertCmd.ExecuteScalarAsync();
        
        await using var selectCmd = new NpgsqlCommand("""
            SELECT id, name, description, created_at
            FROM integration_test_items
            WHERE id = @id
            """, _fixture.Connection);
        selectCmd.Parameters.AddWithValue("id", insertedId);
        
        await using var reader = await selectCmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        
        Assert.Equal(insertedId, reader.GetInt32(0));
        Assert.Equal("Select Test", reader.GetString(1));
        
        _output.WriteLine($"Selected: ID={reader.GetInt32(0)}, Name={reader.GetString(1)}");
    }

    [Fact]
    public async Task Update_ShouldSucceed()
    {
        Assert.NotNull(_fixture.Connection);
        
        await using var insertCmd = new NpgsqlCommand("""
            INSERT INTO integration_test_items (name)
            VALUES (@name)
            RETURNING id
            """, _fixture.Connection);
        insertCmd.Parameters.AddWithValue("name", "Original Name");
        var insertedId = (int)await insertCmd.ExecuteScalarAsync()!;
        
        await using var updateCmd = new NpgsqlCommand("""
            UPDATE integration_test_items
            SET name = @name, description = @description
            WHERE id = @id
            """, _fixture.Connection);
        updateCmd.Parameters.AddWithValue("id", insertedId);
        updateCmd.Parameters.AddWithValue("name", "Updated Name");
        updateCmd.Parameters.AddWithValue("description", "Updated description");
        
        var affectedRows = await updateCmd.ExecuteNonQueryAsync();
        Assert.Equal(1, affectedRows);
        
        await using var verifyCmd = new NpgsqlCommand("""
            SELECT name, description FROM integration_test_items WHERE id = @id
            """, _fixture.Connection);
        verifyCmd.Parameters.AddWithValue("id", insertedId);
        
        await using var reader = await verifyCmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        
        Assert.Equal("Updated Name", reader.GetString(0));
        Assert.Equal("Updated description", reader.GetString(1));
        
        _output.WriteLine($"Updated item ID {insertedId} to 'Updated Name'");
    }

    [Fact]
    public async Task Delete_ShouldSucceed()
    {
        Assert.NotNull(_fixture.Connection);
        
        await using var insertCmd = new NpgsqlCommand("""
            INSERT INTO integration_test_items (name)
            VALUES (@name)
            RETURNING id
            """, _fixture.Connection);
        insertCmd.Parameters.AddWithValue("name", "To Be Deleted");
        var insertedId = (int)await insertCmd.ExecuteScalarAsync()!;
        
        await using var deleteCmd = new NpgsqlCommand("""
            DELETE FROM integration_test_items WHERE id = @id
            """, _fixture.Connection);
        deleteCmd.Parameters.AddWithValue("id", insertedId);
        
        var affectedRows = await deleteCmd.ExecuteNonQueryAsync();
        Assert.Equal(1, affectedRows);
        
        await using var verifyCmd = new NpgsqlCommand("""
            SELECT COUNT(*) FROM integration_test_items WHERE id = @id
            """, _fixture.Connection);
        verifyCmd.Parameters.AddWithValue("id", insertedId);
        
        var count = (long)(await verifyCmd.ExecuteScalarAsync()!)!;
        Assert.Equal(0, count);
        
        _output.WriteLine($"Deleted item ID {insertedId}");
    }

    [Fact]
    public async Task FullCrudCycle_ShouldSucceed()
    {
        Assert.NotNull(_fixture.Connection);
        
        var testName = $"CRUD Test {DateTime.UtcNow:HHmmss}";
        
        var insertCmd = new NpgsqlCommand("""
            INSERT INTO integration_test_items (name, description)
            VALUES (@name, @description)
            RETURNING id
            """, _fixture.Connection);
        insertCmd.Parameters.AddWithValue("name", testName);
        insertCmd.Parameters.AddWithValue("description", "Testing full CRUD cycle");
        var id = (int)(await insertCmd.ExecuteScalarAsync())!;
        await insertCmd.DisposeAsync();
        
        var readCmd = new NpgsqlCommand("""
            SELECT id, name, description FROM integration_test_items WHERE name = @name
            """, _fixture.Connection);
        readCmd.Parameters.AddWithValue("name", testName);
        
        var reader = await readCmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(testName, reader.GetString(1));
        var itemId = reader.GetInt32(0);
        await reader.CloseAsync();
        await readCmd.DisposeAsync();
        
        var updatedName = $"{testName} Updated";
        var updateCmd = new NpgsqlCommand("""
            UPDATE integration_test_items SET name = @name WHERE id = @id
            """, _fixture.Connection);
        updateCmd.Parameters.AddWithValue("id", itemId);
        updateCmd.Parameters.AddWithValue("name", updatedName);
        await updateCmd.ExecuteNonQueryAsync();
        await updateCmd.DisposeAsync();
        
        var verifyCmd = new NpgsqlCommand("""
            SELECT name FROM integration_test_items WHERE id = @id
            """, _fixture.Connection);
        verifyCmd.Parameters.AddWithValue("id", itemId);
        var updatedNameResult = (string)(await verifyCmd.ExecuteScalarAsync()!)!;
        await verifyCmd.DisposeAsync();
        Assert.Equal(updatedName, updatedNameResult);
        
        var deleteCmd = new NpgsqlCommand("""
            DELETE FROM integration_test_items WHERE id = @id
            """, _fixture.Connection);
        deleteCmd.Parameters.AddWithValue("id", itemId);
        await deleteCmd.ExecuteNonQueryAsync();
        await deleteCmd.DisposeAsync();
        
        _output.WriteLine($"Full CRUD cycle completed for ID {itemId}");
    }
}
