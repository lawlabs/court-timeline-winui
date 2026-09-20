using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace CourtTimeline;

internal static class TimelineTheme
{
    internal static Brush Get(FrameworkElement owner, string name, bool highContrast)
    {
        string theme = highContrast ? "HighContrast" : owner.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        string key = "CourtTimeline" + name + "Brush";
        if (Find(owner.Resources, theme, key) is { } local) return local;
        if (Find(Application.Current?.Resources, theme, key) is { } app) return app;
        if (highContrast)
        {
            var settings = new UISettings();
            return new SolidColorBrush(settings.UIElementColor(name switch
            {
                "Shell" or "Surface" or "Today" => UIElementType.Window,
                "Brand" or "Now" => UIElementType.Highlight,
                _ => UIElementType.WindowText
            }));
        }
        bool dark = owner.ActualTheme == ElementTheme.Dark;
        uint rgb = name switch
        {
            "Shell" => dark ? 0x1D2229u : 0xF5F6F8u,
            "Surface" => dark ? 0x242A33u : 0xFFFFFFu,
            "Muted" => dark ? 0xA0AAB8u : 0x646D7Cu,
            "Line" => dark ? 0x39424Eu : 0xDDE2E9u,
            "SubtleLine" => dark ? 0x2C343Fu : 0xF3F4F7u,
            "Brand" => dark ? 0x80C8AEu : 0x347967u,
            "Today" => dark ? 0x263B37u : 0xF1F8F5u,
            "Now" => dark ? 0xFF929Au : 0xB73540u,
            _ => dark ? 0xEDF1F7u : 0x252D3Au
        };
        return Rgb(rgb);
    }

    internal static Brush Tone(FrameworkElement owner, CourtStageTone tone, bool highContrast, bool faint = false)
    {
        if (highContrast) return Get(owner, faint ? "Surface" : "Text", true);
        bool dark = owner.ActualTheme == ElementTheme.Dark;
        uint rgb = tone switch
        {
            CourtStageTone.Blue => dark ? 0x7CB3F1u : 0x3979BBu,
            CourtStageTone.Purple => dark ? 0xB5A1DEu : 0x77629Fu,
            _ => dark ? 0x80C8AEu : 0x347967u
        };
        return Rgb(rgb, faint ? (byte)32 : (byte)255);
    }

    private static SolidColorBrush Rgb(uint rgb, byte alpha = 255) => new(
        Color.FromArgb(alpha, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    private static Brush? Find(ResourceDictionary? dictionary, string theme, string key)
    {
        if (dictionary is null) return null;
        if (dictionary.ThemeDictionaries.TryGetValue(theme, out var value) && value is ResourceDictionary themed
            && themed.TryGetValue(key, out var themedBrush) && themedBrush is Brush result) return result;
        if (dictionary.TryGetValue(key, out var direct) && direct is Brush brush) return brush;
        for (int i = dictionary.MergedDictionaries.Count - 1; i >= 0; i--)
            if (Find(dictionary.MergedDictionaries[i], theme, key) is { } merged) return merged;
        return null;
    }
}
