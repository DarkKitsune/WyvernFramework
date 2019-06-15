using System;
using WyvernFramework;
using WyvernFramework.Commands;
using VulkanCore;
using Spectrum;
using Demos.Effects;
using Demos.RenderPasses;

namespace Demos.Scenes
{
    /// <summary>
    /// The demo menu scene
    /// </summary>
    public class MenuScene : Scene
    {
        public override string Description => "The demo menu scene";

        /// <summary>
        /// Command buffers to clear the swapchain images
        /// </summary>
        private CommandBuffer[] ClearCommandBuffers;

        /// <summary>
        /// Semaphore to be signaled when we're done clearing the image
        /// </summary>
        private Semaphore ClearedSemaphore;

        /// <summary>
        /// Triangle render pass
        /// </summary>
        private BasicRenderPass TriangleRenderPass;

        /// <summary>
        /// Effect for drawing test triangle
        /// </summary>
        private TriangleTestEffect TriangleEffect;

        public MenuScene(WyvernWindow window) : base("Menu", window)
        {
        }

        /// <summary>
        /// Called when starting the scene
        /// </summary>
        public override void OnStart()
        {
            // Create a clear command buffer per swapchain image
            ClearCommandBuffers = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, Graphics.SwapchainAttachmentImages.Length);
            // Create a semaphore for when we're done clearing
            ClearedSemaphore = Graphics.Device.CreateSemaphore();
            // Create render pass
            TriangleRenderPass = new BasicRenderPass(Graphics);
            // Create and start triangle effect
            TriangleEffect = new TriangleTestEffect(Graphics, TriangleRenderPass);
            TriangleEffect.Start();
            TriangleEffect.RegisterSwapchain();
        }

        /// <summary>
        /// Called when ending the scene
        /// </summary>
        public override void OnEnd()
        {
            // Dispose of clear buffers
            foreach (var buffer in ClearCommandBuffers)
                buffer.Dispose();
            // Dispose of clear semaphore
            ClearedSemaphore.Dispose();
            // Dispose of render pass
            TriangleRenderPass.Dispose();
            // End triangle effect
            TriangleEffect.End();
        }

        /// <summary>
        /// Called when updating the scene
        /// </summary>
        public override void OnUpdate()
        {
            // Update clear color for each swapchain image
            for (var imageIndex = 0; imageIndex < Graphics.SwapchainAttachmentImages.Length; imageIndex++)
                SetClearColor(imageIndex);
        }

        /// <summary>
        /// Called when drawing the scene
        /// </summary>
        /// <param name="imageIndex"></param>
        /// <param name="start"></param>
        /// <param name="finished"></param>
        public override void OnDraw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            // Clear screen by clearing the swapchain image
            ClearScreen(start, imageIndex);
            // Draw triangle
            TriangleEffect.Draw(ClearedSemaphore, Graphics.SwapchainAttachmentImages[imageIndex]);
            // We are finished when the triangle is drawn
            finished = TriangleEffect.FinishedSemaphore;
        }

        /// <summary>
        /// Set the clear color for a swapchain image
        /// </summary>
        /// <param name="imageIndex"></param>
        private void SetClearColor(int imageIndex)
        {
            // Create the clear command buffer for the image
            {
                // Generate clear color based on time
                var hue = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond) * 45.0;
                var color = new Color.HSV(hue % 360.0, 1.0, 1.0).ToRGB();
                var clearColor = new ClearColorValue(color.R / 255f, color.G / 255f, color.B / 255f);
                // Record clear command buffer
                {
                    // Acquire image and command buffer
                    var image = Graphics.SwapchainAttachmentImages[imageIndex];
                    var buffer = ClearCommandBuffers[imageIndex];
                    // Specify subresource range we will be working with
                    var range = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
                    // Begin recording
                    buffer.Begin();
                    // Generate commands
                    var commands =
                            new ClearColorCommand(image.Image, range, clearColor)
                        +   new TransitionImageCommand(image.Image, range, ImageLayout.ColorAttachmentOptimal, Accesses.ColorAttachmentWrite);
                    commands.RecordTo(buffer);
                    // Finish recording
                    buffer.End();
                }
            }
        }

        /// <summary>
        /// Clear a swapchain image
        /// </summary>
        /// <param name="start"></param>
        /// <param name="imageIndex"></param>
        private void ClearScreen(Semaphore start, int imageIndex)
        {
            // Submit the clear command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.Transfer, ClearCommandBuffers[imageIndex], ClearedSemaphore
                );
        }
    }
}
