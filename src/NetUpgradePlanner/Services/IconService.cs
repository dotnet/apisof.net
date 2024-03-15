using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace NetUpgradePlanner.Services;

internal sealed class IconService
{
    private readonly Dictionary<string, BitmapImage> _images = new Dictionary<string, BitmapImage>();

    public ImageSource? GetIcon(IconKind icon)
    {
        if (icon == IconKind.None)
            return null;

        var imageUri = GetUri(icon);

        if (!_images.TryGetValue(imageUri, out var result))
        {
            result = new BitmapImage(new Uri(imageUri));
            _images.Add(imageUri, result);
        }

        return result;
    }

    private static string GetUri(IconKind icon)
    {
        var iconName = icon.ToString();
        return GetUri(iconName);
    }

    private static string GetUri(string iconName)
    {
        var assemblyName = typeof(IconService).Assembly.GetName().Name;
        var uri = string.Format("pack://application:,,,/{0};component/Resources/{1}.png", assemblyName, iconName);
        return uri;
    }
}