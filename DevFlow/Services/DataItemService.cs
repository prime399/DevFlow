using System.Net.Http.Json;
using DevFlow.Shared;

namespace DevFlow.Services;

public class DataItemService : IDataItemService
{
    private readonly HttpClient _httpClient;

    public DataItemService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<DataItem>> GetAllAsync(CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync("api/items", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<DataItem>>(ct) ?? [];
    }

    public async Task<DataItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/items/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DataItem>(ct);
    }

    public async Task<DataItem> CreateAsync(DataItem item, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/items", item, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DataItem>(ct) ?? item;
    }

    public async Task<bool> UpdateAsync(Guid id, DataItem item, CancellationToken ct = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/items/{id}", item, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/items/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
