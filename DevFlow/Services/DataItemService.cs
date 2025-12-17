using System.Net.Http.Json;
using System.Text.Json;
using DevFlow.Serialization;
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
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(json, DevFlowJsonContext.Default.IEnumerableDataItem) ?? [];
    }

    public async Task<DataItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"api/items/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(json, DevFlowJsonContext.Default.DataItem);
    }

    public async Task<DataItem> CreateAsync(DataItem item, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(item, DevFlowJsonContext.Default.DataItem);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/items", content, ct);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(responseJson, DevFlowJsonContext.Default.DataItem) ?? item;
    }

    public async Task<bool> UpdateAsync(Guid id, DataItem item, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(item, DevFlowJsonContext.Default.DataItem);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync($"api/items/{id}", content, ct);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"api/items/{id}", ct);
        return response.IsSuccessStatusCode;
    }
}
