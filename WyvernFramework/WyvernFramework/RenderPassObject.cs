using VulkanCore;
using System;

namespace WyvernFramework
{
    /// <summary>
    /// Wraps a RenderPass
    /// </summary>
    public class RenderPassObject : IDebug, IDisposable
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the object
        /// </summary>
        public string Description => "Wraps a RenderPass";

        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The Graphics object associated with the render pass
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// The information used to create the RenderPass object
        /// </summary>
        public RenderPassCreateInfo CreateInfo { get; }

        /// <summary>
        /// The RenderPass object
        /// </summary>
        public RenderPass RenderPass { get; }

        public RenderPassObject(string name, Graphics graphics, RenderPassCreateInfo createInfo)
        {
            Name = name;
            Graphics = graphics;
            CreateInfo = createInfo;
            RenderPass = Graphics.Device.CreateRenderPass(createInfo);
        }

        /// <summary>
        /// Get the attachment description of the given attachment
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public AttachmentDescription GetAttachment(int n)
        {
            if (n < 0 || n >= CreateInfo.Attachments.Length)
                throw new ArgumentOutOfRangeException(nameof(n));
            var attachments = CreateInfo.Attachments;
            return attachments[n];
        }

        /// <summary>
        /// Get the subpass description of the given subpass
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public SubpassDescription GetSubpass(int n)
        {
            if (n < 0 || n >= CreateInfo.Subpasses.Length)
                throw new ArgumentOutOfRangeException(nameof(n));
            var subpasses = CreateInfo.Subpasses;
            return subpasses[n];
        }

        /// <summary>
        /// Create a frame buffer for an attachment image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public Framebuffer CreateFramebuffer(VKImage image)
        {
            // Check arguments
            if (image is null)
                throw new ArgumentNullException(nameof(image));
            // Create framebuffer
            return RenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    new[] { image.ImageView }, image.Extent.Width, image.Extent.Height
                ));
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            OnDispose();
            RenderPass.Dispose();
            Disposed = true;
        }

        /// <summary>
        /// Called when disposed
        /// </summary>
        protected virtual void OnDispose()
        {

        }
    }
}
