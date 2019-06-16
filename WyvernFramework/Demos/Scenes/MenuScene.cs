using System;
using WyvernFramework;
using VulkanCore;
using Spectrum;
using Demos.GraphicalEffects;
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
        /// Triangle render pass
        /// </summary>
        private BasicRenderPass TriangleRenderPass;

        /// <summary>
        /// Effect for clearing an image
        /// </summary>
        private ClearEffect ClearEffect;

        /// <summary>
        /// Effect for drawing test triangle
        /// </summary>
        private TriangleTestEffect TriangleEffect;

        /// <summary>
        /// Effect for transitioning an image before presenting
        /// </summary>
        private TransitionEffect TransitionEffect;

        public MenuScene(WyvernWindow window) : base("Menu", window)
        {
            Content.Add<Texture2D>("TriangleTexture", "test.png");
        }

        /// <summary>
        /// Called when starting the scene
        /// </summary>
        public override void OnStart()
        {
            // Create render pass
            TriangleRenderPass = new BasicRenderPass(Graphics);
            // Create and start a clear effect
            ClearEffect = new ClearEffect(Graphics, TriangleRenderPass);
            ClearEffect.Start();
            // Create and start triangle effect
            TriangleEffect = new TriangleTestEffect(
                    Graphics,
                    TriangleRenderPass,
                    Content["TriangleTexture"] as Texture2D,
                    ClearEffect.FinalLayout,
                    ClearEffect.FinalAccess,
                    ClearEffect.FinalStage
                );
            TransitionEffect = new TransitionEffect(
                    Graphics,
                    TriangleEffect.FinalLayout,
                    TriangleEffect.FinalAccess,
                    TriangleEffect.FinalStage,
                    ImageLayout.ColorAttachmentOptimal,
                    Accesses.MemoryRead,
                    PipelineStages.BottomOfPipe
                );
            TriangleEffect.Start();
        }

        /// <summary>
        /// Called when ending the scene
        /// </summary>
        public override void OnEnd()
        {
            // Dispose of render pass
            TriangleRenderPass.Dispose();
            // End clear effect
            ClearEffect.End();
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
            // Clear screen
            ClearEffect.Draw(start, Graphics.SwapchainAttachmentImages[imageIndex]);
            // Draw triangle
            TriangleEffect.Draw(ClearEffect.FinishedSemaphore, Graphics.SwapchainAttachmentImages[imageIndex]);
            // We are finished when the triangle is drawn
            finished = TriangleEffect.FinishedSemaphore;
        }

        /// <summary>
        /// Set the clear color for a swapchain image
        /// </summary>
        /// <param name="imageIndex"></param>
        private void SetClearColor(int imageIndex)
        {
            // Generate clear color based on time
            var hue = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond) * 45.0;
            var color = new Color.HSV(hue % 360.0, 1.0, 1.0).ToRGB();
            ClearEffect.ClearColor = new ClearColorValue(color.R / 255f, color.G / 255f, color.B / 255f);
        }
    }
}
