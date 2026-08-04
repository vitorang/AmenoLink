using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace AmenoLink.Shared;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { OrderPropertiesAlphabetically }
        }
    };

    public static readonly JsonSerializerOptions CompactOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { OrderPropertiesAlphabetically }
        }
    };

    private static void OrderPropertiesAlphabetically(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        var orderedProperties = typeInfo.Properties.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        typeInfo.Properties.Clear();
        foreach (var prop in orderedProperties)
            typeInfo.Properties.Add(prop);
    }
}
