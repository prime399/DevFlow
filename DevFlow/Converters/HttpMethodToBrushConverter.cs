using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace DevFlow.Converters;

public class HttpMethodToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not string method)
            return Application.Current.Resources["TextPrimaryBrush"];

        return method.ToUpperInvariant() switch
        {
            "GET" => Application.Current.Resources["HttpGetBrush"],
            "POST" => Application.Current.Resources["HttpPostBrush"],
            "PUT" => Application.Current.Resources["HttpPutBrush"],
            "PATCH" => Application.Current.Resources["HttpPatchBrush"],
            "DELETE" => Application.Current.Resources["HttpDeleteBrush"],
            "HEAD" => Application.Current.Resources["HttpHeadBrush"],
            "OPTIONS" => Application.Current.Resources["HttpOptionsBrush"],
            _ => Application.Current.Resources["TextPrimaryBrush"]
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
