using VulkanCore;

namespace WyvernFramework.Commands
{
    /// <summary>
    /// Struct representing an image and layout needed by a command
    /// </summary>
    public class CommandImageLayout
    {
        /// <summary>
        /// The image
        /// </summary>
        public Image Image { get; }

        /// <summary>
        /// The image subresource range
        /// </summary>
        public ImageSubresourceRange Range { get; }

        /// <summary>
        /// The layout of the image
        /// </summary>
        public ImageLayout Layout { get; }

        /// <summary>
        /// How the image will be accessed
        /// </summary>
        public Accesses Access { get; }

        public CommandImageLayout(Image image, ImageSubresourceRange range, ImageLayout layout, Accesses access)
        {
            Image = image;
            Range = range;
            Layout = layout;
            Access = access;
        }
    }
}
