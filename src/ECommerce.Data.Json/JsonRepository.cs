using System.Reflection;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;

namespace ECommerce.Data.Json;

/// <summary>
/// Generic repository over the in-memory DatabaseFile. Mutations serialize the whole
/// store back to disk — trivially correct for a demo, and invisible behind IRepository&lt;T&gt;
/// when this project is later replaced by an EF Core implementation.
/// </summary>
public class JsonRepository<T> : IRepository<T> where T : Entity
{
    private static readonly Dictionary<Type, string> CollectionProperties = new()
    {
        [typeof(Category)] = nameof(DatabaseFile.Categories),
        [typeof(Product)] = nameof(DatabaseFile.Products),
        [typeof(AppUser)] = nameof(DatabaseFile.Users),
        [typeof(Cart)] = nameof(DatabaseFile.Carts),
        [typeof(Order)] = nameof(DatabaseFile.Orders),
        [typeof(Review)] = nameof(DatabaseFile.Reviews),
        [typeof(WishlistItem)] = nameof(DatabaseFile.WishlistItems),
        [typeof(Coupon)] = nameof(DatabaseFile.Coupons),
        [typeof(EmailMessage)] = nameof(DatabaseFile.Emails)
    };

    private readonly JsonDataStore _store;
    private readonly List<T> _list;

    public JsonRepository(JsonDataStore store)
    {
        _store = store;
        var propertyName = CollectionProperties.TryGetValue(typeof(T), out var name)
            ? name
            : throw new NotSupportedException($"No collection mapped for {typeof(T).Name}.");
        var property = typeof(DatabaseFile).GetProperty(propertyName)
            ?? throw new NotSupportedException($"DatabaseFile has no property {propertyName}.");
        _list = (List<T>)property.GetValue(store.Db)!;
    }

    public Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<T> snapshot = _list.ToList();
        return Task.FromResult(snapshot);
    }

    public Task<T?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(_list.FirstOrDefault(e => e.Id == id));

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        _list.Add(entity);
        await _store.SaveAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        _list.AddRange(entities);
        await _store.SaveAsync(ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var index = _list.FindIndex(e => e.Id == entity.Id);
        if (index < 0) _list.Add(entity);
        else _list[index] = entity;
        await _store.SaveAsync(ct);
    }

    public async Task RemoveAsync(string id, CancellationToken ct = default)
    {
        var index = _list.FindIndex(e => e.Id == id);
        if (index >= 0) _list.RemoveAt(index);
        await _store.SaveAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
