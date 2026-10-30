using System.Collections;
using System.Text.Json;

public class CrudServiceAsync<T> : ICrudServiceAsync<T> where T : class, IEntity
{
    private readonly List<T> _items = new();
    private readonly object _sync = new();
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public string FilePath { get; }

    public CrudServiceAsync(string filePath) => FilePath = filePath;

    public Task<bool> CreateAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);
        lock (_sync)
        {
            if (_items.Any(x => x.Id == element.Id)) return Task.FromResult(false);
            _items.Add(element);
            return Task.FromResult(true);
        }
    }

    public Task<T> ReadAsync(Guid id)
    {
        lock (_sync)
            return Task.FromResult(_items.FirstOrDefault(x => x.Id == id)!);
    }

    public Task<IEnumerable<T>> ReadAllAsync()
    {
        lock (_sync)
            return Task.FromResult<IEnumerable<T>>(_items.ToList());   // snapshot
    }

    public Task<IEnumerable<T>> ReadAllAsync(int page, int amount)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        lock (_sync)
            return Task.FromResult<IEnumerable<T>>(
                _items.Skip((page - 1) * amount).Take(amount).ToList());
    }

    public Task<bool> UpdateAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);
        lock (_sync)
        {
            var i = _items.FindIndex(x => x.Id == element.Id);
            if (i < 0) return Task.FromResult(false);
            _items[i] = element;
            return Task.FromResult(true);
        }
    }

    public Task<bool> RemoveAsync(T element)
    {
        ArgumentNullException.ThrowIfNull(element);
        lock (_sync)
            return Task.FromResult(_items.RemoveAll(x => x.Id == element.Id) > 0);
    }

    public async Task<bool> SaveAsync()
    {
        List<T> snapshot;
        lock (_sync) snapshot = _items.ToList();

        await _fileLock.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(FilePath));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            await using var fs = new FileStream(FilePath, FileMode.Create, FileAccess.Write,
                                                FileShare.None, 4096, useAsync: true);
            await JsonSerializer.SerializeAsync(fs, snapshot,
                new JsonSerializerOptions { WriteIndented = true });
            return true;
        }
        catch (IOException) { return false; }
        finally { _fileLock.Release(); }
    }

    // IEnumerable<T>: перебір по знімку, тому колекцію можна змінювати під час foreach
    public IEnumerator<T> GetEnumerator()
    {
        List<T> snapshot;
        lock (_sync) snapshot = _items.ToList();
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}