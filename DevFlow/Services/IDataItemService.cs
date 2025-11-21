using DevFlow.Shared;

namespace DevFlow.Services;

public interface IDataItemService
{
    Task<IEnumerable<DataItem>> GetAllAsync(CancellationToken ct = default);
    Task<DataItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DataItem> CreateAsync(DataItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(Guid id, DataItem item, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
