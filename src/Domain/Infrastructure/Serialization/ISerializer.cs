using System;

namespace Domain.Infrastructure.Serialization
{
    public interface ISerializer
    {
        public byte[] Serialize(object obj);

        public object Deserialize(Type type, byte[] bytes);
    }

    public static class SerializerExtensions
    {
        public static T Deserialize<T>(this ISerializer serializer, byte[] bytes)
        {
            return (T)serializer.Deserialize(typeof(T), bytes);
        }
    }
}
