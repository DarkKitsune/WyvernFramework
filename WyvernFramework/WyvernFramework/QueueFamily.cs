using System;
using System.Collections.Generic;
using System.Linq;
using VulkanCore;
using VulkanCore.Khr;

namespace WyvernFramework
{
    /// <summary>
    /// Class providing a wrapper around a queue family index
    /// </summary>
    public class QueueFamily : IDebug, IDisposable
    {
        /// <summary>
        /// Enum for the possible queue types
        /// </summary>
        public enum QueueType
        {
            Compute,
            Graphics,
            Transfer,
            Present
        }

        /// <summary>
        /// Get the presentation support for a queue family on the platform
        /// </summary>
        /// <param name="device"></param>
        /// <param name="queueFamilyIndex"></param>
        /// <returns></returns>
        private static bool GetPresentationSupport(PhysicalDevice device, int queueFamilyIndex)
        {
            switch (Platform.Type)
            {
                case Platform.PlatformType.Win32:
                    return device.GetWin32PresentationSupportKhr(queueFamilyIndex);
                case Platform.PlatformType.Android:
                case Platform.PlatformType.MacOS:
                    return true;
                default:
                    throw new PlatformNotSupportedException(
                            "Getting presentation support is not implemented for this platform"
                        );
            }
        }

        /// <summary>
        /// Check if a queue family supports a queue type
        /// </summary>
        /// <param name="queueFamily"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool Supports(QueueFamilyProperties queueFamily, Queues type)
        {
            return (queueFamily.QueueFlags & type) != 0;
        }

        /// <summary>
        /// Check if a queue family supports presenting
        /// </summary>
        /// <param name="index"></param>
        /// <param name="device"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        public static bool SupportsPresenting(int index, PhysicalDevice device, SurfaceKhr surface)
        {
            return device.GetSurfaceSupportKhr(index, surface) && GetPresentationSupport(device, index);
        }

        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description of the object
        /// </summary>
        public string Description => "Wrapper around a queue family index";

        /// <summary>
        /// Whether this object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The Graphics object owning this queue family
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// Have the queues been created yet?
        /// </summary>
        public bool HasCreatedQueues { get; private set; }

        /// <summary>
        /// The index of the queue family
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The queues in the queue family
        /// </summary>
        private Queue[] QueueArray { get; }

        /// <summary>
        /// Number of queues created in the queue family
        /// </summary>
        public int Count => QueueArray.Length;

        /// <summary>
        /// The queues in the queue family
        /// </summary>
        public IEnumerable<Queue> Queues => HasCreatedQueues
            ? QueueArray
            : throw new InvalidOperationException("Queue family has not created any queues");

        /// <summary>
        /// The first queue
        /// </summary>
        public Queue First => Count > 0 && HasCreatedQueues
            ? QueueArray[0]
            : throw new InvalidOperationException("Queue family has not created any queues");

        /// <summary>
        /// The highest priority queue available
        /// </summary>
        public Queue HighestPriority => First;

        /// <summary>
        /// The lowest priority queue available
        /// </summary>
        public Queue LowestPriority => Count > 0 && HasCreatedQueues
            ? QueueArray[QueueArray.Length - 1]
            : throw new InvalidOperationException("Queue family has not created any queues");

        /// <summary>
        /// Whether this queue family should have an associated command pool
        /// </summary>
        public bool HasCommandPool { get; }

        /// <summary>
        /// The command pool associated with this queue family
        /// </summary>
        public CommandPool CommandPool { get; private set; }

        /// <summary>
        /// The command pool flags
        /// </summary>
        public CommandPoolCreateFlags CommandPoolFlags { get; }

        /// <summary>
        /// Construct a QueueFamily referring to a queue family that supports a given queue type
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="queueType"></param>
        public QueueFamily(
                string name, Graphics graphics, QueueType queueType, int count,
                bool hasCommandPool, CommandPoolCreateFlags commandPoolFlags = default
            )
        {
            // Argument checks
            if (graphics is null)
                throw new ArgumentNullException(nameof(graphics));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), $"Queue count must be >= 0");
            if (count == 0)
                Debug.Warning($"Queue count for queue family \"{name}\" is 0");
            // Set the object name
            Name = name;
            // Store the Graphics object
            Graphics = graphics;
            // Find a queue family index that fits the requirement
            {
                var families = graphics.PhysicalDevice.GetQueueFamilyProperties();
                Index = -1;
                for (var i = 0; i < families.Length; i++)
                {
                    switch (queueType)
                    {
                        case QueueType.Compute:
                            if (Supports(families[i], VulkanCore.Queues.Compute))
                            {
                                Index = i;
                            }
                            break;
                        case QueueType.Graphics:
                            if (Supports(families[i], VulkanCore.Queues.Graphics))
                            {
                                Index = i;
                            }
                            break;
                        case QueueType.Transfer:
                            if (Supports(families[i], VulkanCore.Queues.Transfer))
                            {
                                Index = i;
                            }
                            break;
                        case QueueType.Present:
                            if (SupportsPresenting(i, graphics.PhysicalDevice, graphics.Surface))
                            {
                                Index = i;
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(queueType));
                    }
                    if (Index != -1)
                        break;
                }
                if (Index == -1)
                {
                    throw new InvalidOperationException(
                            "QueueFamily has an index of -1; no queue family found that matches the criteria?"
                        );
                }
                Debug.Info($"\"{Name}\" created with Index {Index}", nameof(QueueFamily));
            }
            // Create the queue array
            QueueArray = new Queue[count];
            // Set whether to create a command pool
            HasCommandPool = hasCommandPool;
            // Set the command pool flags
            CommandPoolFlags = commandPoolFlags;
        }

        ~QueueFamily()
        {
            // Dispose on finalize
            Dispose();
        }

        /// <summary>
        /// Set the array of queues
        /// </summary>
        /// <param name="queues"></param>
        public void SetQueues(IEnumerable<Queue> queues)
        {
            // Disposed check
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Argument check
            if (queues is null)
                throw new ArgumentNullException(nameof(queues));
            // Set queues
            var i = 0;
            foreach (var queue in queues)
            {
                if (queue is null)
                    throw new NullReferenceException($"Queue cannot be null (queue {i} was null)");
                if (i == QueueArray.Length)
                    break;
                QueueArray[i++] = queue;
            }
            // Flag that the queues are created and set
            HasCreatedQueues = true;
        }

        /// <summary>
        /// Set the command pool
        /// </summary>
        /// <param name="commandPool"></param>
        public void SetCommandPool(CommandPool commandPool)
        {
            // Disposed check
            if (Disposed)
                throw new ObjectDisposedException(Name);
            // Argument check
            if (commandPool is null)
                throw new ArgumentNullException(nameof(commandPool));
            // Don't set if there shouldn't be a command pool
            if (!HasCommandPool)
                throw new InvalidOperationException("The queue family should not have a command pool");
            // Don't allow setting again
            if (!(CommandPool is null))
                throw new InvalidOperationException("The command pool cannot be set more than once");
            // Set the command pool
            CommandPool = commandPool;
        }

        /// <summary>
        /// Create a set of command buffers
        /// </summary>
        /// <param name="level"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public CommandBuffer[] CreateCommandBuffers(CommandBufferLevel level, int count)
        {
            return CommandPool.AllocateBuffers(new CommandBufferAllocateInfo(level, count));
        }

        /// <summary>
        /// Free a set of command buffers
        /// </summary>
        /// <param name="buffers"></param>
        public void FreeCommandBuffers(params CommandBuffer[] buffers)
        {
            CommandPool.FreeBuffers(buffers);
        }

        /// <summary>
        /// Dispose the object and free its resources
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            if (HasCommandPool)
                CommandPool?.Dispose();
        }
    }
}
