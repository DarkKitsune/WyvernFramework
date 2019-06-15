using VulkanCore;
using WyvernFramework;
using System.IO;
using System.Collections.Generic;

namespace Demos.GraphicalEffects
{
    public class ClearEffect : GraphicalEffect
    {
        public ClearEffect(Graphics graphics, RenderPassObject renderPass)
            : base(
                    nameof(TriangleTestEffect), graphics, renderPass,
                    ImageLayout.TransferDstOptimal, Accesses.TransferWrite, PipelineStages.Transfer
                )
        {
        }

        public override void OnStart()
        {
        }

        protected override CommandBuffer OnRegisterImage(AttachmentImage image)
        {
            // Create and record command buffer
            {
                // Create command buffer
                var buffer = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, 1)[0];
                // Begin recording
                buffer.Begin(new CommandBufferBeginInfo());
                // Write commands
                buffer.CmdPipelineBarrier(
                        srcStageMask: InitialStage,
                        dstStageMask: PipelineStages.Transfer,
                        imageMemoryBarriers: new ImageMemoryBarrier[]
                        {
                            new ImageMemoryBarrier(
                                    image: image.Image,
                                    subresourceRange: image.SubresourceRange,
                                    srcAccessMask: InitialAccess,
                                    dstAccessMask: Accesses.TransferWrite,
                                    oldLayout: InitialLayout,
                                    newLayout: ImageLayout.TransferDstOptimal
                                )
                        }
                    );
                buffer.CmdClearColorImage(
                        image.Image,
                        ImageLayout.TransferDstOptimal,
                        new ClearColorValue(1f, 0.5f, 0.5f, 1f),
                        image.SubresourceRange
                    );
                // Finish recording
                buffer.End();
                // Return buffer
                return buffer;
            }
        }

        public override void OnDraw(Semaphore start, AttachmentImage image)
        {
            // Submit the command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.ColorAttachmentOutput, CommandBuffers[image], FinishedSemaphore
                );
        }
    }
}
