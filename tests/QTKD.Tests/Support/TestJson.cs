using System.Text.Json;
using System.Text.Json.Serialization;

namespace QTKD.Tests.Support;

public static class TestJson
{
    // Cùng cách đặt tên (camelCase) và enum dạng chữ như API
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
