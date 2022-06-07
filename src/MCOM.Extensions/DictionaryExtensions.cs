using System;
using System.Collections.Generic;

namespace MCOM.Extensions
{
    public static class DictionaryExtensions
    {
        // Override value of identifical key in base dictionary
        public static void AddRangeOverride<TKey, TValue>(this IDictionary<TKey, TValue> baseDictionary, IDictionary<TKey, TValue> dictionaryToAdd)
        {
            dictionaryToAdd.ForEach(x => baseDictionary[x.Key] = x.Value);
        }

        // Only add new keys into the base dictionary
        public static void AddRangeNewOnly<TKey, TValue>(this IDictionary<TKey, TValue> baseDictionary, IDictionary<TKey, TValue> dictionaryToAdd)
        {
            dictionaryToAdd.ForEach(x =>
            {
                if (!baseDictionary.ContainsKey(x.Key))
                {
                    baseDictionary.Add(x.Key, x.Value);
                }
            });
        }

        // This will throw an error if an existing key from the dictionary to add exists in the base dictionary
        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> baseDictionary, IDictionary<TKey, TValue> dictionaryToAdd)
        {
            dictionaryToAdd.ForEach(x => baseDictionary.Add(x.Key, x.Value));
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }
    }
}
