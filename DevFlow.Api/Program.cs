using DevFlow.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();

// Add CORS for WASM and desktop clients
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// In-memory data store (replace with database in production)
builder.Services.AddSingleton<DataStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

// API Endpoints
var api = app.MapGroup("/api/items");

api.MapGet("/", (DataStore store) => store.GetAll())
   .WithName("GetAllItems");

api.MapGet("/{id:guid}", (Guid id, DataStore store) =>
    store.GetById(id) is { } item ? Results.Ok(item) : Results.NotFound())
   .WithName("GetItemById");

api.MapPost("/", (DataItem item, DataStore store) =>
{
    var created = store.Add(item);
    return Results.Created($"/api/items/{created.Id}", created);
})
.WithName("CreateItem");

api.MapPut("/{id:guid}", (Guid id, DataItem item, DataStore store) =>
    store.Update(id, item) ? Results.NoContent() : Results.NotFound())
   .WithName("UpdateItem");

api.MapDelete("/{id:guid}", (Guid id, DataStore store) =>
    store.Delete(id) ? Results.NoContent() : Results.NotFound())
   .WithName("DeleteItem");

app.Run();

// Simple in-memory data store
public class DataStore
{
    private readonly List<DataItem> _items = new()
    {
        new DataItem { Title = "Sample Item 1", Description = "This is a sample item" },
        new DataItem { Title = "Sample Item 2", Description = "Another sample item", IsCompleted = true }
    };

    public IEnumerable<DataItem> GetAll() => _items.ToList();

    public DataItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);

    public DataItem Add(DataItem item)
    {
        var newItem = item with { Id = Guid.NewGuid(), CreatedAt = DateTime.UtcNow };
        _items.Add(newItem);
        return newItem;
    }

    public bool Update(Guid id, DataItem item)
    {
        var index = _items.FindIndex(x => x.Id == id);
        if (index < 0) return false;
        _items[index] = item with { Id = id, UpdatedAt = DateTime.UtcNow };
        return true;
    }

    public bool Delete(Guid id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        return item != null && _items.Remove(item);
    }
}
