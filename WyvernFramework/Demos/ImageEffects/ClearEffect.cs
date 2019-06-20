using VulkanCore;
using WyvernFramework;
using System.IO;
using System.Collections.Generic;

namespace Demos.GraphicalEffect
{
    public class ClearEffect : ImageEffect
    {
        private ClearColorValue _clearColor = new ClearColorValue(1f, 1f, 1f, 1f);

        /// <summary>
        /// Color to clear with
        /// </summary>
        public ClearColorValue ClearColor
        {
            get { return _clearColor; }
            set
            {
                _clearColor = value;
                Refresh();
            }
        }

        public ClearEffect(Graphics graphics, RenderPassObject renderPass)
            : base(
                    nameof(TriangleTestEffect), graphics,
                    ImageLayout.TransferDstOptimal, Accesses.TransferWrite, PipelineStages.Transfer
                )
        {
        }

        public override void OnStart()
        {
        }

        protected override void OnRecordCommandBuffer(VKImage image, CommandBuffer buffer)
        {
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
                    ClearColor,
                    image.SubresourceRange
                );
            // Finish recording
            buffer.End();
        }

        public override void OnDraw(Semaphore start, VKImage image)
        {
            // Submit the command buffer
            Graphics.TransferQueueFamily.First.Submit(
                    start, PipelineStages.Transfer, GetCommandBuffer(image), FinishedSemaphore
                );
        }
    }
}
