using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using DevFlow.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Controls;

public sealed partial class KeyValueEditorControl : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(KeyValueEditorControl),
            new PropertyMetadata("Items", OnTitleChanged));

    public static readonly DependencyProperty AddButtonLabelProperty =
        DependencyProperty.Register(nameof(AddButtonLabel), typeof(string), typeof(KeyValueEditorControl),
            new PropertyMetadata("Add Item", OnAddButtonLabelChanged));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IList), typeof(KeyValueEditorControl),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty ItemTypeProperty =
        DependencyProperty.Register(nameof(ItemType), typeof(KeyValueItemType), typeof(KeyValueEditorControl),
            new PropertyMetadata(KeyValueItemType.Parameter));

    public KeyValueEditorControl()
    {
        this.InitializeComponent();
        this.Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged collection)
        {
            collection.CollectionChanged -= OnCollectionChanged;
        }
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string AddButtonLabel
    {
        get => (string)GetValue(AddButtonLabelProperty);
        set => SetValue(AddButtonLabelProperty, value);
    }

    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public KeyValueItemType ItemType
    {
        get => (KeyValueItemType)GetValue(ItemTypeProperty);
        set => SetValue(ItemTypeProperty, value);
    }

    public event EventHandler<object>? ItemAdded;
    public event EventHandler<object>? ItemDeleted;
    public event EventHandler? ItemsCleared;

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyValueEditorControl control)
        {
            control.TitleText.Text = e.NewValue as string ?? "Items";
        }
    }

    private static void OnAddButtonLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyValueEditorControl control)
        {
            control.AddButtonText.Text = e.NewValue as string ?? "Add Item";
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KeyValueEditorControl control)
        {
            control.ItemsListControl.ItemsSource = e.NewValue as IList;
            control.UpdateCount();

            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += control.OnCollectionChanged;
            }
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCount();
    }

    private void UpdateCount()
    {
        CountText.Text = (ItemsSource?.Count ?? 0).ToString();
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsSource == null) return;

        object newItem = ItemType switch
        {
            KeyValueItemType.Parameter => new RequestParameter(),
            KeyValueItemType.Header => new RequestHeader(),
            _ => new RequestParameter()
        };

        ItemsSource.Add(newItem);
        ItemAdded?.Invoke(this, newItem);
        UpdateCount();
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is object item && ItemsSource != null)
        {
            if (ItemsSource.Count > 1)
            {
                ItemsSource.Remove(item);
                ItemDeleted?.Invoke(this, item);
            }
            else
            {
                // Clear the last item instead of removing it
                if (item is RequestParameter param)
                {
                    param.ParamKey = string.Empty;
                    param.ParamValue = string.Empty;
                    param.Description = string.Empty;
                    param.IsEnabled = true;
                }
                else if (item is RequestHeader header)
                {
                    header.HeaderKey = string.Empty;
                    header.HeaderValue = string.Empty;
                    header.Description = string.Empty;
                    header.IsEnabled = true;
                }
            }
            UpdateCount();
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        if (ItemsSource == null) return;

        ItemsSource.Clear();

        object newItem = ItemType switch
        {
            KeyValueItemType.Parameter => new RequestParameter(),
            KeyValueItemType.Header => new RequestHeader(),
            _ => new RequestParameter()
        };

        ItemsSource.Add(newItem);
        ItemsCleared?.Invoke(this, EventArgs.Empty);
        UpdateCount();
    }
}

public enum KeyValueItemType
{
    Parameter,
    Header
}
