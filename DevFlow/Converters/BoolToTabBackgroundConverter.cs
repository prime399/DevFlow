using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevFlow.Converters;

public class BoolToTabBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isActive && isActive)
        {
            // Active tab - darker background
            return Application.Current.Resources["RequestBarBackgroundBrush"] as SolidColorBrush 
                ?? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 46));
        }
        // Inactive tab - lighter/transparent
        return new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
