using VulkanCore;
using WyvernFramework;

namespace Demos.RenderPasses
{
    public class BasicRenderPass : RenderPassObject
    {
        public BasicRenderPass(Graphics graphics) : base(
                nameof(BasicRenderPass), graphics,
                new RenderPassCreateInfo(
                        subpasses: new SubpassDescription[]
                        {
                            new SubpassDescription(
                                    flags: 0,
                                    colorAttachments: new AttachmentReference[]
                                    {
                                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                                    }
                                )
                        },
                        attachments: new AttachmentDescription[]
                        {
                            new AttachmentDescription(
                                    flags: 0,
                                    format: graphics.SwapchainImageFormat,
                                    samples: SampleCounts.Count1,
                                    loadOp: AttachmentLoadOp.Load,
                                    storeOp: AttachmentStoreOp.Store,
                                    stencilLoadOp: AttachmentLoadOp.DontCare,
                                    stencilStoreOp: AttachmentStoreOp.DontCare,
                                    initialLayout: ImageLayout.ColorAttachmentOptimal,
                                    finalLayout: ImageLayout.PresentSrcKhr
                                )
                        }
                    )
            )
        {
        }
    }
}
