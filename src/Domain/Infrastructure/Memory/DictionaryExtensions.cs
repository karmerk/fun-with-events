using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Infrastructure.Memory
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey,TValue> dictionary, TKey key, Func<TValue> addFunc)
        {
            if(dictionary.TryGetValue(key, out var value))
            {
                return value;
            }
            
            return dictionary[key] = addFunc();
        }
    }
}
