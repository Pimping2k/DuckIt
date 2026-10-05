using System.Collections.Generic;
using UnityEngine;

namespace Extensions
{
    public static class CollectionExtensions
    {
        public static T GetRandomValue<T>(this List<T> list)
        {
            int randomIndex = Random.Range(0, list.Count);
            T randomValue = list[randomIndex];
            return randomValue;
        }
    }
}