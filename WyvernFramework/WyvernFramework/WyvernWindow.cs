using System;
using System.Numerics;
using System.Collections.Generic;
using VkGLFW3;
using VulkanCore;
using VulkanCore.Khr;

namespace WyvernFramework
{
    /// <summary>
    /// A Wyvern game window
    /// </summary>
    public class WyvernWindow : Window, IDebug
    {
        /// <summary>
        /// Whether WyvernWindow has been initialized
        /// </summary>
        public static bool Initialized { get; private set; }

        /// <summary>
        /// The main window
        /// </summary>
        public static WyvernWindow Main { get; private set; }

        /// <summary>
        /// Get required Vulkan extensions for a window
        /// </summary>
        public static IEnumerable<string> RequiredExtensions
        {
            get
            {
                foreach (var ext in VkGlfw.RequiredInstanceExtensions)
                    yield return ext;
            }
        }

        /// <summary>
        /// Initialize the window system
        /// </summary>
        public static void Init()
        {
            // Don't allow initializing twice
            if (Initialized)
                throw new InvalidOperationException("WyvernWindow is already initialized");
            // Initialize VkGLFW3
            VkGlfw.Init();
            // Check for Vulkan availability
            if (!VkGlfw.VulkanSupported)
                throw new PlatformNotSupportedException("Vulkan unsupported on this machine!");
            // Signal that we are initialized
            Initialized = true;
        }

        /// <summary>
        /// Terminate the window system
        /// </summary>
        public static void Terminate()
        {
            // Don't allow terminating twice
            if (!Initialized)
                throw new InvalidOperationException("WyvernWindow is not initialized");
            // Terminate VkGLFW3
            VkGlfw.Terminate();
            // Signal that we are not initialized
            Initialized = false;
        }

        /// <summary>
        /// The name of this window; usually equal to the title
        /// </summary>
        public string Name => Title;

        /// <summary>
        /// The description of the window
        /// </summary>
        public string Description => this == Main ? "The main Wyvern game window" : "A Wyvern game window";

        /// <summary>
        /// Size of the window as a vector
        /// </summary>
        public Vector2 Size
        {
            get
            {
                var (x, y) = GetSize();
                return new Vector2(x, y);
            }
        }

        /// <summary>
        /// The Graphics object owned by this window
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// Update rate of the app, in updates per second
        /// </summary>
        public double UpdateRate;

        /// <summary>
        /// Current time in seconds
        /// </summary>
        public double CurrentTime => DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond;

        /// <summary>
        /// Last update time
        /// </summary>
        private double LastUpdate;

        /// <summary>
        /// Do we need to update?
        /// </summary>
        private bool NeedsUpdate => CurrentTime >= LastUpdate + 1.0 / UpdateRate;

        /// <summary>
        /// Construct a window with a size and title
        /// </summary>
        /// <param name="size"></param>
        /// <param name="title"></param>
        public WyvernWindow(Vector2 size, string title, double updateRate = 60.0) : base((int)size.X, (int)size.Y, title)
        {
            // Argument exceptions
            if (size.X <= 0f || size.Y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(size), "size X and Y must be greater than 0");
            if (title is null)
                throw new ArgumentNullException(nameof(title));
            // Throw exception if WyvernWindow is not initialized
            if (!Initialized)
                throw new InvalidOperationException("WyvernWindow is not initialized");
            // Create Graphics object
            Graphics = new Graphics(this);
            Graphics.PrintDebug();
            // Set update rate
            UpdateRate = updateRate;
            // Set last update so that an update will occur
            LastUpdate = CurrentTime - 1.0 / UpdateRate;
            // If there is not already a Main window then use this one as the Main window
            if (Main is null)
                Main = this;
        }

        /// <summary>
        /// Construct a window with the default size and title
        /// </summary>
        public WyvernWindow() : this(new Vector2(1280, 720), "Wyvern")
        {
        }

        /// <summary>
        /// Start the window's event system
        /// </summary>
        public void Start()
        {
            // Print debug info
            this.PrintDebug();
            // Main loop
            while (!ShouldClose)
            {
                // Do OS events
                PollEvents();
                // Do update
                while (NeedsUpdate)
                    Update();
                // Do draw
                Draw();
            }
        }

        /// <summary>
        /// Execute the update event
        /// </summary>
        public void Update()
        {
            // Call update event
            OnUpdate();
            // Add to LastUpdate
            LastUpdate += 1.0 / UpdateRate;
        }

        /// <summary>
        /// The update event; update app logic
        /// </summary>
        public virtual void OnUpdate()
        {
        }

        /// <summary>
        /// Execute the draw event
        /// </summary>
        public void Draw()
        {
            // Get the next swapchain image
            var image = Graphics.NextSwapchainImage();
            // Wait for any previous rendering on this image to finish
            Graphics.RenderToImageFences[image].Wait();
            Graphics.RenderToImageFences[image].Reset();
            // Do draw event
            OnDraw(Graphics.ImageAvailableSemaphore, image, out var onDrawEnd);
            Graphics.SignalRenderToImageFence(onDrawEnd, image);
            Graphics.PresentQueueFamily.HighestPriority.PresentKhr(Graphics.ReadyToPresentSemaphore, Graphics.Swapchain, image);
        }

        /// <summary>
        /// The draw event; called when drawing to a swapchain image
        /// </summary>
        protected virtual void OnDraw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            throw new InvalidOperationException("Don't call the base OnDraw method");
        }
    }
}
