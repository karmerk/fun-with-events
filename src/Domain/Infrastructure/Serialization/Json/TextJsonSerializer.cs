using System;
using System.Text;

namespace Domain.Infrastructure.Serialization.Json
{
    public class TextJsonSerializer : ISerializer
    {
        private readonly System.Text.Json.JsonSerializerOptions? _options = null;

        public object Deserialize(Type type, string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize(json, type, _options) ?? throw new InvalidOperationException($"Could not deserialize {type.AssemblyQualifiedName} from JSON=\"{json}\"");
        }

        public string Serialize(object obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _options);
        }
    }
}
