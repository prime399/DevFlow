using DevFlow.Services;
using DevFlow.Shared;
using System.Collections.Immutable;

namespace DevFlow.Presentation;

public partial record MainModel
{
    private readonly INavigator _navigator;
    private readonly IDataItemService _dataItemService;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator,
        IDataItemService dataItemService)
    {
        _navigator = navigator;
        _dataItemService = dataItemService;
        Title = "DevFlow";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
    }

    public string? Title { get; }

    // MVUX Feed for data items - auto-fetches from API
    public IListFeed<DataItem> Items => ListFeed.Async(async ct =>
    {
        var items = await _dataItemService.GetAllAsync(ct);
        return items.ToImmutableList();
    });

    // State for new item input
    public IState<string> NewItemTitle => State<string>.Value(this, () => string.Empty);
    public IState<string> NewItemDescription => State<string>.Value(this, () => string.Empty);

    // State to trigger refresh
    private IState<int> RefreshTrigger => State<int>.Value(this, () => 0);

    public async ValueTask AddItem(CancellationToken ct)
    {
        var title = await NewItemTitle;
        var description = await NewItemDescription;

        if (string.IsNullOrWhiteSpace(title)) return;

        var newItem = new DataItem
        {
            Title = title!,
            Description = description ?? string.Empty
        };

        await _dataItemService.CreateAsync(newItem, ct);

        // Clear inputs
        await NewItemTitle.UpdateAsync(_ => string.Empty, ct);
        await NewItemDescription.UpdateAsync(_ => string.Empty, ct);
    }

    public async ValueTask DeleteItem(DataItem item, CancellationToken ct)
    {
        await _dataItemService.DeleteAsync(item.Id, ct);
    }

    public async ValueTask ToggleComplete(DataItem item, CancellationToken ct)
    {
        var updated = item with { IsCompleted = !item.IsCompleted };
        await _dataItemService.UpdateAsync(item.Id, updated, ct);
    }

    public async Task GoToSecond()
    {
        var title = await NewItemTitle;
        await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(title ?? ""));
    }
}
