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

        public MipMap2D(IntPtr dataPtr, int stride, Extent2D extent, bool flip = true)
        {
            Extent = extent;
            var totalBytes = stride * extent.Height;
            Data = new byte[totalBytes];
            unsafe
            {
                var src = (byte*)dataPtr;
                fixed (byte* destStart = Data)
                {
                    if (flip)
                    {
                        var dest = destStart + totalBytes - stride;
                        for (var i = 0; i < extent.Height; i++)
                        {
                            System.Buffer.MemoryCopy(src, dest, stride, stride);
                            dest -= stride;
                            src += stride;
                        }
                    }
                    else
                    {
                        System.Buffer.MemoryCopy(src, destStart, totalBytes, totalBytes);
                    }
                }
            }
        }
    }
}
