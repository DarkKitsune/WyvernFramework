using System;
using WyvernFramework;
using WyvernFramework.Sprites;
using VulkanCore;
using Spectrum;
using Demos.GraphicalEffect;
using Demos.RenderPasses;
using System.Numerics;

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
        private SpriteEffect SpriteEffect;

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
            SpriteEffect = new SpriteEffect(
                    Graphics,
                    TriangleRenderPass,
                    ClearEffect.FinalLayout,
                    ClearEffect.FinalAccess,
                    ClearEffect.FinalStage
                );
            SpriteEffect.Start();
            var rand = new Random();
            for (var i = 0; i < InstanceList.MaxInstances; i++)
            {
                var vel = new Vector3(
                        -50f + (float)rand.NextDouble() * 100f,
                        -50f + (float)rand.NextDouble() * 100f,
                        0f
                    );
                new SpriteInstance(SpriteEffect, Vector3.Zero, vel, new Vector2(16, 16), Content["TriangleTexture"] as Texture2D, new Rect2D(0, 0, 16, 16));
            }
            TransitionEffect = new TransitionEffect(
                    Graphics,
                    SpriteEffect.FinalLayout,
                    SpriteEffect.FinalAccess,
                    SpriteEffect.FinalStage,
                    ImageLayout.ColorAttachmentOptimal,
                    Accesses.MemoryRead,
                    PipelineStages.BottomOfPipe
                );
            TransitionEffect.Start();
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
            SpriteEffect.End();
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
            SpriteEffect.Draw(ClearEffect.FinishedSemaphore, Graphics.SwapchainAttachmentImages[imageIndex]);
            // We are finished when the triangle is drawn
            finished = SpriteEffect.FinishedSemaphore;
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
