using VulkanCore;
using System;

namespace WyvernFramework
{
    /// <summary>
    /// Represents a color image
    /// </summary>
    public class VKImage : IDebug, IDisposable
    {
        private ImageView _imageView;

        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the object
        /// </summary>
        public string Description => "A Vulkan image";

        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }
        
        /// <summary>
        /// The image
        /// </summary>
        public Image Image { get; }

        /// <summary>
        /// The format of the image
        /// </summary>
        public Format Format { get; }

        /// <summary>
        /// The extent of the image
        /// </summary>
        public Extent2D Extent { get; }

        /// <summary>
        /// Subresource range
        /// </summary>
        public ImageSubresourceRange SubresourceRange { get; }

        /// <summary>
        /// Get a basic ImageView
        /// </summary>
        public ImageView ImageView
        {
            get
            {
                if (Disposed)
                    throw new ObjectDisposedException(Name);
                if (_imageView is null)
                {
                    _imageView = Image.CreateView(new ImageViewCreateInfo(
                            Format,
                            new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)
                        ));
                }
                return _imageView;
            }
        }

        public VKImage(Image image, Format format, Extent2D extent, ImageSubresourceRange subresourceRange)
        {
            Image = image;
            Format = format;
            Extent = extent;
            SubresourceRange = subresourceRange;
        }

        ~VKImage()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            _imageView?.Dispose();
            Image.Dispose();
        }
    }
}
