using System;
using System.Collections.Generic;
using System.Linq;
using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// Class wrapping around a set of Vulkan objects that perform a set of commands
    /// </summary>
    public class ImageEffect : IDebug, IDisposable
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description of the object
        /// </summary>
        public virtual string Description => "Wraps around a set of Vulkan objects that perform a set of commands";

        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Whether the effect has been started
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// The Graphics object the effect is associated with
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// The command buffer registry
        /// </summary>
        protected Dictionary<VKImage, CommandBuffer> CommandBuffers { get; } = new Dictionary<VKImage, CommandBuffer>();

        /// <summary>
        /// Get all registered images
        /// </summary>
        public IEnumerable<VKImage> RegisteredImages => CommandBuffers.Keys;

        /// <summary>
        /// Get all registered images and their command buffers
        /// </summary>
        public IEnumerable<KeyValuePair<VKImage, CommandBuffer>> RegisteredPairs => CommandBuffers;

        /// <summary>
        /// The semaphore that should be signaled when the effect finishes its tasks
        /// </summary>
        public Semaphore FinishedSemaphore { get; private set; }

        /// <summary>
        /// The initial image layout
        /// </summary>
        protected ImageLayout InitialLayout { get; }

        /// <summary>
        /// The initial image access type
        /// </summary>
        protected Accesses InitialAccess { get; }

        /// <summary>
        /// The earliest pipeline stage the pipeline is allowed to operate in
        /// (ideally, the final stage of a previous effect this effect relies on)
        /// </summary>
        protected PipelineStages InitialStage { get; }

        /// <summary>
        /// The layout an image should be in after the effect finishes
        /// </summary>
        public ImageLayout FinalLayout { get; }

        /// <summary>
        /// The access type an image should be in after the effect finishes
        /// </summary>
        public Accesses FinalAccess { get; }

        /// <summary>
        /// The latest pipeline stage the effect takes place in
        /// </summary>
        public PipelineStages FinalStage { get; }

        public ImageEffect(
                string name, Graphics graphics,
                ImageLayout finalLayout, Accesses finalAccess, PipelineStages finalStage, ImageLayout initialLayout = ImageLayout.Undefined,
                Accesses initialAccess = Accesses.None, PipelineStages initialStage = PipelineStages.TopOfPipe
            )
        {
            Name = name;
            Graphics = graphics;
            InitialLayout = initialLayout;
            InitialAccess = initialAccess;
            InitialStage = initialStage;
            FinalLayout = finalLayout;
            FinalAccess = finalAccess;
            FinalStage = finalStage;
        }

        /// <summary>
        /// Start the effect
        /// </summary>
        public void Start()
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Don't allow starting twice
            if (Active)
                throw new InvalidOperationException("Effect is already active");
            // Create semaphore for when we're done
            FinishedSemaphore = Graphics.Device.CreateSemaphore();
            // Set to active and run OnStart
            Active = true;
            OnStart();
        }

        /// <summary>
        /// Called when starting the effect
        /// </summary>
        public virtual void OnStart()
        {
        }

        /// <summary>
        /// End the effect
        /// </summary>
        public void End()
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Don't allow ending if not active
            if (Active)
                throw new InvalidOperationException("Effect is not active");
            // Unregister all images
            foreach (var kvp in CommandBuffers)
                UnregisterImage(kvp.Key);
            // Dispose of semaphore
            FinishedSemaphore.Dispose();
            // Run OnEnd and set to inactive
            OnEnd();
            Active = false;
        }

        /// <summary>
        /// Called when ending the effect
        /// </summary>
        public virtual void OnEnd()
        {
        }

        /// <summary>
        /// Draw the effect
        /// </summary>
        public void Draw(Semaphore start, VKImage image)
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Make sure this effect is active
            if (!Active)
                throw new InvalidOperationException("Effect is not active");
            // Call OnDraw
            OnDraw(start, image);
        }

        /// <summary>
        /// Called when drawing the effect
        /// </summary>
        public virtual void OnDraw(Semaphore start, VKImage image = null)
        {
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            // End the effect if active
            if (Active)
                End();
            Disposed = true;
        }

        /// <summary>
        /// Register an image for rendering to
        /// </summary>
        /// <param name="image"></param>
        public CommandBuffer RegisterImage(VKImage image)
        {
            // Check arguments
            if (image is null)
                throw new ArgumentNullException(nameof(image));
            // Check if active
            if (!Active)
                throw new InvalidOperationException("Effect is not active");
            // Create new command buffer, register the image, and reord the command buffer
            var cmd = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, 1)[0];
            CommandBuffers.Add(image, cmd);
            OnRegisterImage(image);
            OnRecordCommandBuffer(image, cmd);
            return cmd;
        }

        /// <summary>
        /// Register all swapchain images owned by the associated Graphics object
        /// </summary>
        public void RegisterSwapchain()
        {
            foreach (var image in Graphics.SwapchainAttachmentImages)
                RegisterImage(image);
        }

        /// <summary>
        /// Called when registering an image
        /// </summary>
        /// <param name="image"></param>
        /// <returns>New command buffer for the image, or null</returns>
        protected virtual void OnRegisterImage(VKImage image)
        {
        }

        /// <summary>
        /// Called to record to a command buffer
        /// </summary>
        /// <param name="image"></param>
        /// <param name="buffer"></param>
        protected virtual void OnRecordCommandBuffer(VKImage image, CommandBuffer buffer)
        {
        }

        /// <summary>
        /// Unregister an image from the effect
        /// </summary>
        /// <param name="image"></param>
        public void UnregisterImage(VKImage image)
        {
            // Check arguments
            if (image is null)
                throw new ArgumentNullException(nameof(image));
            // Check if active
            if (!Active)
                throw new InvalidOperationException("Effect is not active");
            // Call OnUnregisterImage and unregister command buffer
            Graphics.Device.WaitIdle();
            OnUnregisterImage(image);
            CommandBuffers[image].Dispose();
            CommandBuffers.Remove(image);
        }

        /// <summary>
        /// Called when registering an image
        /// </summary>
        /// <param name="image"></param>
        protected virtual void OnUnregisterImage(VKImage image)
        {
        }

        /// <summary>
        /// Get the command buffer created for a registered image
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public CommandBuffer GetCommandBuffer(VKImage image)
        {
            // Check arguments
            if (image is null)
                throw new ArgumentNullException(nameof(image));
            if (CommandBuffers.TryGetValue(image, out var buffer))
                return buffer;
            return RegisterImage(image);
        }

        /// <summary>
        /// Re-record command buffers
        /// </summary>
        public void Refresh()
        {
            Graphics.Device.WaitIdle();
            foreach (var image in RegisteredImages.ToArray())
            {
                OnRecordCommandBuffer(image, CommandBuffers[image]);
            }
        }
    }
}
