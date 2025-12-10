using System.ComponentModel;

namespace DevFlow.Models;

public interface IKeyValueItem : INotifyPropertyChanged
{
    Guid Id { get; }
    bool IsEnabled { get; set; }
    string Key { get; set; }
    string Value { get; set; }
    string Description { get; set; }
}
