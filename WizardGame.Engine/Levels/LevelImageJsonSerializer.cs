using System.Text.Json;
using System.Text.Json.Serialization;

namespace WizardGame.Engine;

public static class LevelImageJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(LevelImageData data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return JsonSerializer.Serialize(data, Options);
    }

    public static LevelImageData Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Level JSON is required.", nameof(json));
        }

        var data = JsonSerializer.Deserialize<LevelImageData>(json, Options);
        if (data is null)
        {
            throw new FormatException("Level JSON could not be parsed.");
        }

        return data;
    }
}
