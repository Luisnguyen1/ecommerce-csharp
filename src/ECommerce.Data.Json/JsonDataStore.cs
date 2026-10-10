using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECommerce.Data.Json;

/// <summary>
/// Owns database.json: loads it once into memory, serializes every mutation back to disk
/// atomically (temp file + rename) under a single-writer lock.
/// </summary>
public class JsonDataStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly DatabaseFile _db;

    public JsonDataStore(string path)
    {
        _path = path;
        _db = Load() ?? new DatabaseFile();
    }

    public DatabaseFile Db => _db;

    private DatabaseFile? Load()
    {
        if (!File.Exists(_path)) return null;
        try
        {
            return JsonSerializer.Deserialize<DatabaseFile>(File.ReadAllText(_path), Options);
        }
        catch (JsonException)
        {
            // Corrupt file: back it up rather than crash the app.
            File.Copy(_path, _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), overwrite: true);
            return null;
        }
    }

    public bool IsEmpty => _db.Categories.Count == 0 && _db.Products.Count == 0 && _db.Users.Count == 0;

    /// <summary>Runs the seeder when the file is fresh, then persists.</summary>
    public async Task EnsureSeededAsync(Func<DatabaseFile, Task> seed)
    {
        await _lock.WaitAsync();
        try
        {
            if (!IsEmpty) return;
            await seed(_db);
            await WriteUnderLockAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await WriteUnderLockAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task WriteUnderLockAsync()
    {
        var temp = _path + ".tmp";
        await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(_db, Options));
        File.Move(temp, _path, overwrite: true);
    }
}
