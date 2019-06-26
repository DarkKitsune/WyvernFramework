using System.Numerics;
using VulkanCore;
using WyvernFramework;
using Demos.Scenes;

namespace Demos
{
    /// <summary>
    /// The app window
    /// </summary>
    public class AppWindow : WyvernWindow
    {
        private MenuScene MenuScene { get; }

        /// <summary>
        /// App window constructor
        /// </summary>
        public AppWindow() : base(new Vector2(1500, 1000), "Test App", 30.0)
        {
            // Create the menu scene
            MenuScene = new MenuScene(this);
            // Start the menu scene
            MenuScene.Start();
        }

        /// <summary>
        /// Called when updating logic
        /// </summary>
        public override void OnUpdate()
        {
            // Update the menu scene
            MenuScene.Update();
        }

        /// <summary>
        /// Called when drawing to a swapchain image
        /// </summary>
        /// <param name="start">The semaphore signaling when drawing should start</param>
        /// <param name="imageIndex">The swapchain image index</param>
        /// <param name="finished">The semaphore we will signal when drawing is done</param>
        protected override void OnDraw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            // Draw the menu scene
            MenuScene.Draw(start, imageIndex, out finished);
        }
    }
}
