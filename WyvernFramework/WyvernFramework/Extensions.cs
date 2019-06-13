using System.Collections.Generic;
using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// Class providing useful extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Append a value to an enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> enumerable, T element)
        {
            foreach (T e in enumerable)
                yield return e;
            yield return element;
        }

        /// <summary>
        /// Prepend a value to an enumerable
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumerable"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> enumerable, T element)
        {
            yield return element;
            foreach (T e in enumerable)
                yield return e;
        }
    }
}
