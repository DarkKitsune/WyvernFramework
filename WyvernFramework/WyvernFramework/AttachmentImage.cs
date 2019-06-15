using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// Represents part of an image that can be used for attachments
    /// </summary>
    public class AttachmentImage
    {
        private ImageView _imageView;

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

        public AttachmentImage(Image image, Format format, Extent2D extent, ImageSubresourceRange subresourceRange)
        {
            Image = image;
            Format = format;
            Extent = extent;
            SubresourceRange = subresourceRange;
        }
    }
}
