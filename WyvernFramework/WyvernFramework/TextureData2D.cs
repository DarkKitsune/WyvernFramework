using System.Collections.Generic;
using System.Linq;
using System;
using VulkanCore;

namespace WyvernFramework
{
    public class TextureData2D
    {
        /// <summary>
        /// The mipmaps making up the texture data
        /// </summary>
        public MipMap2D[] MipMaps { get; }

        /// <summary>
        /// The format of the image
        /// </summary>
        public Format Format => MipMaps[0].Format;

        /// <summary>
        /// Size of all mipmaps in bytes
        /// </summary>
        public long Size => MipMaps.Sum(e => e.Size);

        public TextureData2D(IEnumerable<MipMap2D> mipmaps)
        {
            if (!mipmaps.Any())
                throw new ArgumentException("Texture requires at least 1 mipmap", nameof(mipmaps));
            MipMaps = mipmaps.ToArray();
        }
    }
}
