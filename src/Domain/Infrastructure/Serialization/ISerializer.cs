using System;

namespace Domain.Infrastructure.Serialization
{
    public interface ISerializer
    {
        public string Serialize(object obj);

        public object Deserialize(Type type, string text);
    }

    public static class SerializerExtensions
    {
        public static T Deserialize<T>(this ISerializer serializer, string text)
        {
            return (T)serializer.Deserialize(typeof(T), text);
        }
    }
}
