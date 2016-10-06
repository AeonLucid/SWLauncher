using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLauncher.Extensions
{
    internal static class DictionaryExtensions
    {
        private static readonly Random Random = new Random();

        public static Dictionary<TKey, TValue> Shuffle<TKey, TValue>(this Dictionary<TKey, TValue> source)
        {
            return source.OrderBy(x => Random.Next())
               .ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
