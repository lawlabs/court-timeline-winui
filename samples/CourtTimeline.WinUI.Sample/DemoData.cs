using System.Text.Json;
using System.Text.Json.Serialization;

namespace CourtTimeline.Sample;

internal static class DemoData
{
    internal static CourtTimelineData Load()
    {
        using var stream = typeof(DemoData).Assembly.GetManifestResourceStream("CourtTimeline.Sample.Demo.json")
            ?? throw new InvalidOperationException("Demo fixture is missing.");
        return JsonSerializer.Deserialize(stream, DemoJsonContext.Default.CourtTimelineData)
            ?? throw new InvalidOperationException("Demo fixture is empty.");
    }
}

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(CourtTimelineData))]
internal partial class DemoJsonContext : JsonSerializerContext;
