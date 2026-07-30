using System.Collections.Generic;
using System.Text.Json;

namespace Brio.Resources;

public static class Localize
{
    private static readonly Dictionary<string, string> _stringDb = [];

    public static string Get(string key, string? defaultValue = null)
    {
        if(_stringDb.TryGetValue(key, out var value))
            return value;
        return defaultValue ?? key;
    }

    public static string? GetNullable(string key)
    {
        if(_stringDb.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public static void Load(ResourceProvider provider)
    {
        _stringDb.Clear();
        LoadFile(provider, "Language.en.json", false);
        LoadFile(provider, "Language.zh-TW.json", true);
    }

    private static void LoadFile(ResourceProvider provider, string name, bool replace)
    {
        var raw = provider.GetRawResourceString(name);
        using JsonDocument doc = JsonDocument.Parse(raw);
        FlattenRecursive(doc.RootElement, "", replace);
    }

    private static void FlattenRecursive(JsonElement element, string currentKey, bool replace)
    {
        switch(element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach(var property in element.EnumerateObject())
                {
                    FlattenRecursive(property.Value, $"{currentKey}{property.Name}.", replace);
                }
                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach(var item in element.EnumerateArray())
                {
                    FlattenRecursive(item, $"{currentKey}{index}.", replace);
                    index++;
                }
                break;
            default:
                var key = currentKey.TrimEnd('.');
                if(replace)
                    _stringDb[key] = element.ToString();
                else
                    _stringDb.Add(key, element.ToString());
                break;
        }
    }
}
