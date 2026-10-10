namespace ECommerce.Core.Interfaces;

/// <summary>
/// Persistence seam. Today: JSON file. Tomorrow: swap the implementation for
/// EF Core (one project) without touching services or pages.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<T?> GetByIdAsync(string id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task RemoveAsync(string id, CancellationToken ct = default);
    /// <summary>Flushes pending changes to the underlying store (no-op for auto-saving implementations).</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
