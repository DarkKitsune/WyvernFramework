using System.Collections.Generic;
using VulkanCore;
using Spectrum;
using System;

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

        /// <summary>
        /// Convert Spectrum.Color.RGB to VulkanCore.ColorF4
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ColorF4 ToVulkanCore(this Color.RGB color)
        {
            return new ColorF4(color.R / 255f, color.G / 255f, color.B / 255f, 1f);
        }

        /// <summary>
        /// Convert Spectrum.Color.HSV to VulkanCore.ColorF4
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ColorF4 ToVulkanCore(this Color.HSV color)
        {
            return color.ToRGB().ToVulkanCore();
        }

        /// <summary>
        /// Convert Spectrum.Color.HSL to VulkanCore.ColorF4
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static ColorF4 ToVulkanCore(this Color.HSL color)
        {
            return color.ToRGB().ToVulkanCore();
        }

        /// <summary>
        /// Convert VulkanCore.ColorF4 to Spectrum.Color.RGB
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color.RGB ToSpectrum(this ColorF4 color)
        {
            return new Color.RGB((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
        }

        /// <summary>
        /// Align an offset using std140 alignment rules
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int AlignSTD140<T>(this int offset)
            where T : struct
        {
            var size = Interop.SizeOf<T>();
            var align = (int)Math.Ceiling((double)size / sizeof(float)) * sizeof(float);
            return (int)Math.Ceiling((double)offset / align) * align;
        }
    }
}
