using System.Numerics;
using VulkanCore;
using WyvernFramework;

namespace Demos
{
    /// <summary>
    /// The app window
    /// </summary>
    public class AppWindow : WyvernWindow
    {
        // The semaphore that will be signaled after drawing is done
        private Semaphore OnDrawEndSemaphore { get; }
        // The command buffers that clear the images
        private CommandBuffer[] ClearCommandBuffers { get; }

        /// <summary>
        /// App window constructor
        /// </summary>
        public AppWindow() : base(new Vector2(1280, 720), "Test App")
        {
            // Create draw end semaphore
            OnDrawEndSemaphore = Graphics.Device.CreateSemaphore();
            // Create clear image command buffers
            {
                // Create buffers
                ClearCommandBuffers = Graphics.GraphicsQueueFamily.CreateCommandBuffers(
                    CommandBufferLevel.Primary, Graphics.SwapchainImageCount
                );
                // Record buffers
                for (var i = 0; i < ClearCommandBuffers.Length; i++)
                {
                    // Get the command buffer
                    var buffer = ClearCommandBuffers[i];
                    // Get the image
                    var image = Graphics.Swapchain.GetImages()[i];
                    // Record the command buffer
                    {
                        // Begin recording
                        buffer.Begin(new CommandBufferBeginInfo());
                        // We need the image prepared for transferring to, so transition it with a barrier
                        buffer.CmdPipelineBarrier(
                                srcStageMask: PipelineStages.TopOfPipe, /* We don't need anything done first so let's allow
                                                                        this to start super early */
                                dstStageMask: PipelineStages.Transfer,   /* We will be transferring to the image so it must be
                                                                        done before the transfer stage */
                                imageMemoryBarriers: new[] { new ImageMemoryBarrier(
                                    image: image,                                   // Use the swapchain image
                                    subresourceRange: new ImageSubresourceRange(    // Transition the color aspect
                                            ImageAspects.Color, 0, 1, 0, 1
                                        ),
                                    srcAccessMask: Accesses.None,               // We don't care what the image was previously used for
                                    dstAccessMask: Accesses.TransferWrite,      // We will be transferring to the image
                                    oldLayout: ImageLayout.Undefined,           // We don't care what the image's old layout was
                                    newLayout: ImageLayout.TransferDstOptimal   // We will be transferring to the image
                                ) }
                            );
                        // Clear the image color
                        buffer.CmdClearColorImage(
                                image: image,                                   // Clear the swapchain image
                                imageLayout: ImageLayout.TransferDstOptimal,    // The image is in transfer destination optimal layout
                                color: new ClearColorValue(0.5f, 0.7f, 1f, 1f), // Clear to light blue
                                ranges: new ImageSubresourceRange(              // Clear the color aspect
                                        ImageAspects.Color, 0, 1, 0, 1
                                    )
                            );
                        // We need to transition to the optimal layout for presenting
                        buffer.CmdPipelineBarrier(
                                srcStageMask: PipelineStages.Transfer,      // We just transferred so don't transition before that stage
                                dstStageMask: PipelineStages.BottomOfPipe,  /* We will be presenting the image so it can be done any time
                                                                            before the bottom of the pipeline */
                                imageMemoryBarriers: new[] { new ImageMemoryBarrier(
                                    image: image,                                   // Use the swapchain image
                                    subresourceRange: new ImageSubresourceRange(    // Transition the color aspect
                                            ImageAspects.Color, 0, 1, 0, 1
                                        ),
                                    srcAccessMask: Accesses.TransferWrite,      // We just transferred to the image so that was the previous usage
                                    dstAccessMask: Accesses.MemoryRead,         // Presenting the image will require reading its memory
                                    oldLayout: ImageLayout.TransferDstOptimal,  // The old layout was transfer destination optimal layout
                                    newLayout: ImageLayout.PresentSrcKhr        /* We will be presenting the image so put it in present
                                                                                    source layout */
                                ) }
                            );
                        // Stop recording to buffer
                        buffer.End();
                    }
                }
            }
        }

        /// <summary>
        /// Called when drawing
        /// </summary>
        /// <param name="start">The semaphore signaling when drawing should start</param>
        /// <param name="end">The semaphore we will signal when drawing is done</param>
        protected override void OnDraw(Semaphore start, int imageIndex, out Semaphore end)
        {
            // Submit the clear command buffer for the image after the start semaphore is signaled
            Graphics.GraphicsQueueFamily.HighestPriority.Submit(new SubmitInfo(
                    waitSemaphores: new[] { start },                            // Wait for start to be signaled
                    waitDstStageMask: new[] { PipelineStages.Transfer },        /* The first thing we need to do is transfer to the image,
                                                                                    so wait in that stage */
                    commandBuffers: new[] { ClearCommandBuffers[imageIndex] },  // We are submitting the clear command buffer for this image
                    signalSemaphores: new[] { OnDrawEndSemaphore }              // Signal OnDrawEndSemaphore when this finishes
                ));
            // We will be signalng OnDrawEndSemaphore at the end of drawing
            end = OnDrawEndSemaphore;
        }
    }
}
