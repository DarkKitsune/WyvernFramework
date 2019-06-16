using VulkanCore;
using System;

namespace WyvernFramework
{
    /// <summary>
    /// Class representing mipmap data
    /// </summary>
    public class MipMap2D
    {
        /// <summary>
        /// The mipmap image data
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// The mipmap extent
        /// </summary>
        public Extent2D Extent { get; }

        /// <summary>
        /// Format of the mipmap
        /// </summary>
        public Format Format => Format.B8G8R8A8UNorm;

        /// <summary>
        /// The size of the mipmap in bytes
        /// </summary>
        public long Size => (long)Extent.Width * Extent.Height * PixelSize;

        /// <summary>
        /// Size in bytes of each pixel
        /// </summary>
        public int PixelSize => 4;

        public MipMap2D(IntPtr dataPtr, int dataSize, Extent2D extent)
        {
            Extent = extent;
            var destSize = extent.Width * extent.Height * PixelSize;
            Data = new byte[destSize];
            unsafe
            {
                var src = (byte*)dataPtr;
                fixed (byte* dest = Data)
                {
                    System.Buffer.MemoryCopy(src, dest, destSize, dataSize);
                }
            }
        }
    }
}
