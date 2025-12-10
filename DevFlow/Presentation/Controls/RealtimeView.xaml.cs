using System.Text;
using DevFlow.Models;
using DevFlow.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace DevFlow.Presentation.Controls;

public sealed partial class RealtimeView : UserControl
{
    private Button[] _protocolButtons = null!;

    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(nameof(ViewModel), typeof(RealtimeViewModel), typeof(RealtimeView),
            new PropertyMetadata(null, OnViewModelChanged));

    public RealtimeView()
    {
        this.InitializeComponent();
        _protocolButtons = new Button[] { ProtocolWebSocket, ProtocolSSE, ProtocolSocketIO };
    }

    public RealtimeViewModel? ViewModel
    {
        get => (RealtimeViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RealtimeView view && e.NewValue is RealtimeViewModel vm)
        {
            view.BindViewModel(vm);
        }
    }

    private void BindViewModel(RealtimeViewModel vm)
    {
        RealtimeTabsControl.ItemsSource = vm.TabManager.Tabs;
        SyncActiveTabToUI();
    }

    private void Protocol_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string protocolTag && ViewModel?.TabManager.ActiveTab != null)
        {
            var protocol = protocolTag switch
            {
                "WebSocket" => RealtimeProtocol.WebSocket,
                "SSE" => RealtimeProtocol.SSE,
                "SocketIO" => RealtimeProtocol.SocketIO,
                _ => RealtimeProtocol.WebSocket
            };

            ViewModel.TabManager.ActiveTab.Protocol = protocol;

            foreach (var btn in _protocolButtons)
            {
                btn.Style = btn == button
                    ? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                    : (Style)Application.Current.Resources["RequestTabStyle"];
            }

            UpdateUrlPlaceholder(protocol);
            UpdateSendButtonVisibility(protocol);
        }
    }

    private void UpdateUrlPlaceholder(RealtimeProtocol protocol)
    {
        UrlTextBox.PlaceholderText = protocol switch
        {
            RealtimeProtocol.WebSocket => "wss://ws.postman-echo.com/raw",
            RealtimeProtocol.SSE => "https://stream.wikimedia.org/v2/stream/recentchange",
            RealtimeProtocol.SocketIO => "https://socket-io-chat.now.sh",
            _ => "wss://ws.postman-echo.com/raw"
        };
    }

    private void UpdateSendButtonVisibility(RealtimeProtocol protocol)
    {
        SendButton.Visibility = protocol == RealtimeProtocol.SSE ? Visibility.Collapsed : Visibility.Visible;
    }

    private void AddRealtimeTab_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TabManager.AddNewTab();
        SyncActiveTabToUI();
    }

    private void RealtimeTab_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.DataContext is RealtimeTab tab && ViewModel != null)
        {
            ViewModel.TabManager.SetActiveTab(tab);
            SyncActiveTabToUI();
        }
    }

    private void CloseRealtimeTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is RealtimeTab tab && ViewModel != null)
        {
            ViewModel.TabManager.CloseTab(tab);
            SyncActiveTabToUI();
        }
    }

    private void RealtimeTabName_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement rename dialog
    }

    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        var tab = ViewModel.TabManager.ActiveTab;
        if (tab == null) return;

        if (tab.IsConnected)
        {
            await ViewModel.DisconnectAsync(CancellationToken.None);
            UpdateConnectButton(false);
            SendButton.IsEnabled = false;
        }
        else
        {
            SyncUIToActiveTab();
            ConnectingProgress.IsActive = true;
            ConnectButton.IsEnabled = false;

            try
            {
                await ViewModel.ConnectAsync(CancellationToken.None);
                UpdateConnectButton(tab.IsConnected);
                SendButton.IsEnabled = tab.IsConnected && tab.Protocol != RealtimeProtocol.SSE;
            }
            finally
            {
                ConnectingProgress.IsActive = false;
                ConnectButton.IsEnabled = true;
            }
        }

        UpdateLogList();
    }

    private async void Send_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        SyncUIToActiveTab();
        await ViewModel.SendMessageAsync(CancellationToken.None);
        UpdateLogList();
    }

    private void ClearInput_Click(object sender, RoutedEventArgs e)
    {
        MessageEditor.Clear();
    }

    private void UpdateConnectButton(bool isConnected)
    {
        ConnectButtonText.Text = isConnected ? "Disconnect" : "Connect";
        ConnectButton.Style = isConnected
            ? (Style)Application.Current.Resources["DisconnectButtonStyle"]
            : (Style)Application.Current.Resources["ConnectButtonStyle"];
    }

    private void SyncActiveTabToUI()
    {
        var tab = ViewModel?.TabManager.ActiveTab;
        if (tab == null) return;

        UrlTextBox.Text = tab.Url;
        MessageEditor.Text = tab.Message;
        UpdateConnectButton(tab.IsConnected);
        SendButton.IsEnabled = tab.IsConnected && tab.Protocol != RealtimeProtocol.SSE;
        UpdateUrlPlaceholder(tab.Protocol);
        UpdateSendButtonVisibility(tab.Protocol);
        UpdateProtocolButtons(tab.Protocol);
        UpdateLogList();
    }

    private void SyncUIToActiveTab()
    {
        var tab = ViewModel?.TabManager.ActiveTab;
        if (tab == null) return;

        tab.Url = UrlTextBox.Text;
        tab.Message = MessageEditor.Text;
    }

    private void UpdateProtocolButtons(RealtimeProtocol protocol)
    {
        var index = protocol switch
        {
            RealtimeProtocol.WebSocket => 0,
            RealtimeProtocol.SSE => 1,
            RealtimeProtocol.SocketIO => 2,
            _ => 0
        };

        for (int i = 0; i < _protocolButtons.Length; i++)
        {
            _protocolButtons[i].Style = i == index
                ? (Style)Application.Current.Resources["RequestTabActiveStyle"]
                : (Style)Application.Current.Resources["RequestTabStyle"];
        }
    }

    private void UpdateLogList()
    {
        var tab = ViewModel?.TabManager.ActiveTab;
        LogList.ItemsSource = tab?.Logs;
    }

    private void CopyLogs_Click(object sender, RoutedEventArgs e)
    {
        var tab = ViewModel?.TabManager.ActiveTab;
        if (tab?.Logs == null) return;

        var sb = new StringBuilder();
        foreach (var log in tab.Logs)
        {
            sb.AppendLine($"[{log.TimestampFormatted}] {log.Type}: {log.Message}");
            if (!string.IsNullOrEmpty(log.Details))
                sb.AppendLine($"  Details: {log.Details}");
        }

        var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
        dataPackage.SetText(sb.ToString());
        Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.TabManager.ActiveTab?.ClearLogs();
        UpdateLogList();
    }
}
