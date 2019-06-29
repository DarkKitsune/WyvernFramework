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
            ClearEffect.ClearColor = new ClearColorValue(0.5f, 0.7f, 0.9f);
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
            var tex = Content["TriangleTexture"] as Texture2D;
            var anims = new[] {
                    new Animation(new[] {
                        Animation.Instruction.SetScale(0f, new Vector2(12f, 12f)),
                        Animation.Instruction.LerpScale(0f, 1f, new Vector2(30f, 10f)),
                        Animation.Instruction.LerpScale(1f, 1f, new Vector2(8f, 20f)),
                        Animation.Instruction.LerpScale(2f, 1f, new Vector2(12f, 12f)),
                        Animation.Instruction.None(3f)
                }),
                    new Animation(new[] {
                        Animation.Instruction.SetScale(0f, new Vector2(26f, 26f)),
                        Animation.Instruction.LerpScale(0f, 1f, new Vector2(8f, 30f)),
                        Animation.Instruction.LerpScale(1f, 1f, new Vector2(20f, 8f)),
                        Animation.Instruction.LerpScale(2f, 1f, new Vector2(26f, 26f)),
                        Animation.Instruction.None(3f)
                })
            };

            for (var i = 0; i < InstanceList.MaxInstances; i++)
            {
                var vel = new Vector3(
                        -75f + (float)rand.NextDouble() * 150f,
                        -75f + (float)rand.NextDouble() * 150f,
                        0f
                    );
                var pos = new Vector3(
                        -100f + (float)rand.NextDouble() * 200f,
                        -100f + (float)rand.NextDouble() * 200f,
                       -1f + (float)rand.NextDouble() * 2f
                    );
                new SpriteInstance(SpriteEffect, pos, vel, new Vector2(32, 32), tex, new Rect2D(0, 0, 32, 32), anims[i % 2]);
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
    }
}
