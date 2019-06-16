using System;
using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// A class representing an app scene
    /// </summary>
    public class Scene : IDebug, IDisposable
    {
        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the object
        /// </summary>
        public virtual string Description => "An app scene.";

        /// <summary>
        /// Whether the scene is active (has been started)
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// The window the scene is attached to
        /// </summary>
        public WyvernWindow Window { get; }

        /// <summary>
        /// The Graphics object associated with the window the scene belongs to
        /// </summary>
        public Graphics Graphics => Window.Graphics;

        public Scene(string name, WyvernWindow window)
        {
            // Check arguments
            if (window is null)
                throw new ArgumentNullException(nameof(window));
            // Set fields
            Name = name;
            Window = window;
        }

        ~Scene()
        {
            Dispose();
        }

        /// <summary>
        /// Start the scene
        /// </summary>
        public void Start()
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Don't allow starting twice
            if (Active)
                throw new InvalidOperationException("Scene is already active");
            // Set to active and run OnStart
            Active = true;
            OnStart();
        }

        /// <summary>
        /// Called when starting the scene
        /// </summary>
        public virtual void OnStart()
        {
        }

        /// <summary>
        /// End the scene
        /// </summary>
        public void End()
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Don't allow ending if not active
            if (Active)
                throw new InvalidOperationException("Scene is not active");
            // Run OnEnd and set to inactive
            OnEnd();
            Active = false;
        }

        /// <summary>
        /// Called when ending the scene
        /// </summary>
        public virtual void OnEnd()
        {
        }

        /// <summary>
        /// Update the scene
        /// </summary>
        public void Update()
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Make sure this scene is active
            if (!Active)
                throw new InvalidOperationException("Scene is not active");
            OnUpdate();
        }

        /// <summary>
        /// Called when updating the scene
        /// </summary>
        public virtual void OnUpdate()
        {
        }

        /// <summary>
        /// Draw the scene
        /// </summary>
        public void Draw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            // Check if disposed
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Make sure this scene is active
            if (!Active)
                throw new InvalidOperationException("Scene is not active");
            OnDraw(start, imageIndex, out finished);
        }

        /// <summary>
        /// Called when drawing the scene
        /// </summary>
        public virtual void OnDraw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            finished = null;
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            // Exit if already disposed
            if (Disposed)
                return;
            // End the scene if active
            if (Active)
                End();
            // Flag that we're disposed
            Disposed = true;
        }
    }
}
