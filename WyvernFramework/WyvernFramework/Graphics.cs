using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VulkanCore;
using VulkanCore.Ext;
using VulkanCore.Khr;
using System.Numerics;

namespace WyvernFramework
{
    /// <summary>
    /// Class providing a vulkan context to do graphics commands with
    /// </summary>
    public class Graphics : IDebug
    {
        /// <summary>
        /// The enabled Vulkan instance extensions
        /// </summary>
        public static string[] EnabledInstanceExtensions { get; } = new[]
        {
            Constant.InstanceExtension.ExtDebugReport
        };

        /// <summary>
        /// The enabled Vulkan device extensions
        /// </summary>
        public static string[] EnabledDeviceExtensions { get; } = new[]
        {
            Constant.DeviceExtension.KhrSwapchain
        };

        /// <summary>
        /// The enabled Vulkan debug layers extensions
        /// </summary>
        public static string[] EnabledDebugLayers { get; } = new[]
        {
            Constant.InstanceLayer.LunarGStandardValidation
        };

        /// <summary>
        /// The preferred swapchain formats, in order
        /// </summary>
        public static Format[] PreferredSwapchainFormats { get; } = new[] {
                Format.B8G8R8A8UNorm,
                Format.B4G4R4A4UNormPack16
            };

        /// <summary>
        /// The preferred presentation modes, in order
        /// </summary>
        public static PresentModeKhr[] PreferredPresentModes { get; } = new[] {
                PresentModeKhr.Mailbox,
                PresentModeKhr.FifoRelaxed
            };

        /// <summary>
        /// The enabled physical device features
        /// </summary>
        public static PhysicalDeviceFeatures EnabledPhysicalDeviceFeatures { get; } = new PhysicalDeviceFeatures
        {
        };

        /// <summary>
        /// The compute command pool flags
        /// </summary>
        public static CommandPoolCreateFlags ComputeCommandPoolFlags { get; } =
            CommandPoolCreateFlags.ResetCommandBuffer;

        /// <summary>
        /// The graphics command pool flags
        /// </summary>
        public static CommandPoolCreateFlags GraphicsCommandPoolFlags { get; } =
            CommandPoolCreateFlags.ResetCommandBuffer;

        /// <summary>
        /// The transfer command pool flags
        /// </summary>
        public static CommandPoolCreateFlags TransferCommandPoolFlags { get; } =
            CommandPoolCreateFlags.ResetCommandBuffer
            | CommandPoolCreateFlags.Transient; // Will probably tend to be used for only a very short time

        /// <summary>
        /// Whether the static part of Graphics has been initialized
        /// </summary>
        private static bool InitializedStatic;

        /// <summary>
        /// The Vulkan instance
        /// </summary>
        public static Instance Instance { get; private set; }

        /// <summary>
        /// The debug report callback
        /// </summary>
        public static DebugReportCallbackExt DebugCallback { get; private set; }

        /// <summary>
        /// Initialize the static part of Graphics
        /// </summary>
        private static void InitializeStatic()
        {
            // Don't do anything if it's already been initialized
            if (InitializedStatic)
                return;
            // Create the Vulkan Instance
            {
                var createInfo = new InstanceCreateInfo();
                // Set application info automatically using assembly info and Vulkan API 1.0.0
                {
                    var entryAssemblyName = Assembly.GetEntryAssembly().GetName();
                    var thisAssemblyName = typeof(Graphics).Assembly.GetName();
                    createInfo.ApplicationInfo = new ApplicationInfo()
                    {
                        ApiVersion = new VulkanCore.Version(1, 0, 0),
                        ApplicationName = entryAssemblyName.Name,
                        ApplicationVersion = new VulkanCore.Version(
                                    entryAssemblyName.Version.Major,
                                    entryAssemblyName.Version.Minor,
                                    entryAssemblyName.Version.Build
                                ),
                        EngineName = "Wyvern",
                        EngineVersion = new VulkanCore.Version(
                                    thisAssemblyName.Version.Major,
                                    thisAssemblyName.Version.Minor,
                                    thisAssemblyName.Version.Build
                                )
                    };
                }
                // Set the enabled extensions
                createInfo.EnabledExtensionNames = EnabledInstanceExtensions
                        .Concat(WyvernWindow.RequiredExtensions)
                        .Distinct()
                        .ToArray();
                // Set the enabled layers
                var availableLayers = Instance.EnumerateLayerProperties();
                createInfo.EnabledLayerNames = EnabledDebugLayers
                    .Where(availableLayers.Contains)
                    .Distinct()
                    .ToArray();
                // Create instance
                Instance = new Instance(createInfo);
            }
            // Create the debug callback
            {
                DebugCallback = Instance.CreateDebugReportCallbackExt(
                new DebugReportCallbackCreateInfoExt()
                {
                    Callback = OnDebugReport
                });
            }
            // Flag that we are now initialized
            InitializedStatic = true;
        }

        /// <summary>
        /// Called for debug reports
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static bool OnDebugReport(DebugReportCallbackInfo info)
        {
            // Is this an error?
            var error = (info.Flags & DebugReportFlagsExt.Error) != 0;
            // Is this a warning?
            var warning = (info.Flags & DebugReportFlagsExt.Warning) != 0
                || (info.Flags & DebugReportFlagsExt.PerformanceWarning) != 0;
            // Choose print method
            Action<string, string> printMethod = Debug.Info;
            if (error)
                printMethod = Debug.Error;
            else if (warning)
                printMethod = Debug.Warning;
            // Print the report
            printMethod($"{info.Message} (#{info.MessageCode})", $"Layer {info.LayerPrefix}; Object {info.Object}");
            // Return true if this was an error
            return error;
        }

        /// <summary>
        /// Get whether a physical device meets requirement features and capabilities
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private static bool GetDeviceMeetsRequirements(PhysicalDevice device, SurfaceKhr surface)
        {
            // Argument checks
            if (device is null)
                throw new ArgumentNullException(nameof(device));
            if (surface is null)
                throw new ArgumentNullException(nameof(surface));
            // Check device features
            {
                var features = device.GetFeatures();
                if (!features.SamplerAnisotropy)
                    return false;
                var limits = device.GetProperties().Limits;
                if (limits.MaxVertexInputAttributes < 16)
                    return false;
                if (limits.MaxUniformBufferRange < sizeof(float) * 4 * 254)
                    return false;
                if (limits.MaxVertexInputBindings < 2)
                    return false;
                if (limits.MaxPointSize <= 1)
                    return false;
            }
            // Check queue families
            {
                var families = device.GetQueueFamilyProperties();
                // Graphics, compute, transfer
                {
                    if (families.Count(family => QueueFamily.Supports(family, Queues.Graphics)) == 0
                        || families.Count(family => QueueFamily.Supports(family, Queues.Compute)) == 0
                        || families.Count(family => QueueFamily.Supports(family, Queues.Transfer)) == 0)
                    {
                        return false;
                    }
                }
                // Present
                {
                    var foundPresent = false;
                    for (var i = 0; i < families.Length; i++)
                    {
                        if (QueueFamily.SupportsPresenting(i, device, surface))
                        {
                            foundPresent = true;
                            break;
                        }
                    }
                    if (!foundPresent)
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Score a physical device based on features and capabilities
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        private static double GetDeviceScore(PhysicalDevice device)
        {
            var limits = device.GetProperties().Limits;
            return limits.MaxVertexInputAttributes / 16.0;
        }

        /// <summary>
        /// The physical device the window uses
        /// </summary>
        public PhysicalDevice PhysicalDevice { get; }

        /// <summary>
        /// The logical device the window uses
        /// </summary>
        public Device Device { get; }

        /// <summary>
        /// The window's surface
        /// </summary>
        public SurfaceKhr Surface { get; }

        /// <summary>
        /// The surface's capabilities
        /// </summary>
        public SurfaceCapabilitiesKhr SurfaceCapabilities => PhysicalDevice.GetSurfaceCapabilitiesKhr(Surface);

        /// <summary>
        /// The surface's supported formats
        /// </summary>
        public IEnumerable<SurfaceFormatKhr> SurfaceFormats => PhysicalDevice.GetSurfaceFormatsKhr(Surface);

        /// <summary>
        /// The surface's present modes
        /// </summary>
        public IEnumerable<PresentModeKhr> SurfacePresentModes => PhysicalDevice.GetSurfacePresentModesKhr(Surface);

        /// <summary>
        /// The window's swapchain
        /// </summary>
        public SwapchainKhr Swapchain { get; }

        /// <summary>
        /// The swapchain dimensions
        /// </summary>
        public Extent2D SwapchainExtent { get; }

        /// <summary>
        /// The swapchain images
        /// </summary>
        public AttachmentImage[] SwapchainAttachmentImages { get; }

        /// <summary>
        /// The swapchain's image format
        /// </summary>
        public Format SwapchainImageFormat { get; }

        /// <summary>
        /// The swapchain's color space
        /// </summary>
        public ColorSpaceKhr SwapchainColorSpace { get; }

        /// <summary>
        /// The swapchain's present mode
        /// </summary>
        public PresentModeKhr SwapchainPresentMode { get; }

        /// <summary>
        /// The default compute queue family
        /// </summary>
        public QueueFamily ComputeQueueFamily { get; }

        /// <summary>
        /// The default graphics queue family
        /// </summary>
        public QueueFamily GraphicsQueueFamily { get; }

        /// <summary>
        /// The default transfer queue family
        /// </summary>
        public QueueFamily TransferQueueFamily { get; }

        /// <summary>
        /// The default present queue family
        /// </summary>
        public QueueFamily PresentQueueFamily { get; }

        public QueueFamily[] QueueFamilies => new[] {
                ComputeQueueFamily, GraphicsQueueFamily, TransferQueueFamily, PresentQueueFamily
            };

        /// <summary>
        /// Get debug strings for this Graphics
        /// </summary>
        public IEnumerable<string> DebugStrings => PhysicalDevice.GetProperties().ToStringDebug()
                .Concat(PhysicalDevice.GetProperties().Limits.ToStringDebug());

        /// <summary>
        /// The WyvernWindow that owns this Graphics object
        /// </summary>
        public WyvernWindow Window { get; }

        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name => $"{Window}'s {nameof(Graphics)}";

        /// <summary>
        /// The description of the object
        /// </summary>
        public string Description => "Provides a vulkan context to do graphics commands with";

        /// <summary>
        /// Semaphore for when the next swapchain image is available
        /// </summary>
        public Semaphore ImageAvailableSemaphore { get; }

        /// <summary>
        /// Fences for when rendering to each swapchain image is complete
        /// </summary>
        public Fence[] RenderToImageFences { get; }

        /// <summary>
        /// Construct a Graphics object belonging to a window
        /// </summary>
        /// <param name="window"></param>
        public Graphics(WyvernWindow window)
        {
            // Argument checks
            if (window is null)
                throw new ArgumentNullException(nameof(window));
            // Initialize the static part of Graphics if necessary
            InitializeStatic();
            // Store window
            Window = window;
            // Create window surface
            {
                AllocationCallbacks? allocationCallbacks = null;
                Surface = new SurfaceKhr(
                        Instance,
                        ref allocationCallbacks,
                        VkGLFW3.VkGlfw.CreateWindowSurface(Instance.Handle, Window, IntPtr.Zero)
                    );
                Surface.PrintDebug();
            }
            // Choose a physical device
            {
                PhysicalDevice = Instance.EnumeratePhysicalDevices()
                    .Where(e => GetDeviceMeetsRequirements(e, Surface))
                    .OrderByDescending(GetDeviceScore)
                    .FirstOrDefault();
                if (PhysicalDevice is null)
                    throw new InvalidOperationException("No physical device found that meets the requirements for the application");
            }
            // Create default queue families
            {
                ComputeQueueFamily = new QueueFamily(
                        $"{Name}'s {nameof(ComputeQueueFamily)}", this, QueueFamily.QueueType.Compute, 2,
                        true, ComputeCommandPoolFlags
                    );
                GraphicsQueueFamily = new QueueFamily(
                        $"{Name}'s {nameof(GraphicsQueueFamily)}", this, QueueFamily.QueueType.Graphics, 2,
                        true, GraphicsCommandPoolFlags
                    );
                TransferQueueFamily = new QueueFamily(
                        $"{Name}'s {nameof(TransferQueueFamily)}", this, QueueFamily.QueueType.Transfer, 2,
                        true, TransferCommandPoolFlags
                    );
                PresentQueueFamily = new QueueFamily(
                        $"{Name}'s {nameof(PresentQueueFamily)}", this, QueueFamily.QueueType.Present, 1,
                        false
                    );
            }
            // Create a logical device
            {
                // Generate queue create info structs
                var queueCreateInfos = QueueFamilies.Select(queueFamily =>
                {
                    // Generate queue priorities
                    var priorities = new float[queueFamily.Count];
                    for (var i = 0; i < priorities.Length; i++)
                    {
                        priorities[i] = 1f - (i / (float)(priorities.Length - 1));
                    }
                    // Create create info
                    var createInfo = new DeviceQueueCreateInfo()
                    {
                        QueueFamilyIndex = queueFamily.Index,
                        QueueCount = queueFamily.Count,
                        QueuePriorities = priorities
                    };
                    return createInfo;
                });
                // Merge multiple queue families' queue create infos
                {
                    var alreadyHave = new List<int>();
                    var uniqueCreateInfos = new List<DeviceQueueCreateInfo>();
                    foreach (var createInfo in queueCreateInfos.OrderByDescending(e => e.QueueCount))
                    {
                        if (!alreadyHave.Contains(createInfo.QueueFamilyIndex))
                        {
                            alreadyHave.Add(createInfo.QueueFamilyIndex);
                            uniqueCreateInfos.Add(createInfo);
                        }
                    }
                    queueCreateInfos = uniqueCreateInfos;
                    foreach (var createInfo in queueCreateInfos)
                        createInfo.PrintDebug();
                }
                // Create device
                Device = PhysicalDevice.CreateDevice(new DeviceCreateInfo()
                {
                    EnabledExtensionNames = EnabledDeviceExtensions.ToArray(),
                    QueueCreateInfos = queueCreateInfos.ToArray(),
                    EnabledFeatures = EnabledPhysicalDeviceFeatures
                });
                // Set the queues in the queue families, using those created with the device
                foreach (var family in QueueFamilies)
                {
                    var queues = new Queue[family.Count];
                    for (var i = 0; i < queues.Length; i++)
                        queues[i] = Device.GetQueue(family.Index, i);
                    family.SetQueues(queues);
                }
                // Create and set the command pools in the queue families
                foreach (var family in QueueFamilies)
                {
                    // Skip family that shouldn't have a command pool
                    if (!family.HasCommandPool)
                        continue;
                    // Create command pool
                    var commandPool = Device.CreateCommandPool(new CommandPoolCreateInfo()
                    {
                        QueueFamilyIndex = family.Index,
                        Flags = family.CommandPoolFlags
                    });
                    family.SetCommandPool(commandPool);
                }
            }
            // Create swapchain
            {
                // Query supported capabilities and formats
                var surfaceCapabilities = SurfaceCapabilities;
                var surfaceFormats = SurfaceFormats;
                var surfacePresentModes = SurfacePresentModes;
                surfaceCapabilities.PrintDebug();
                // Choose the best image format and color space
                {
                    var imageFormat = Array.Find(
                        PreferredSwapchainFormats,
                        preferred => surfaceFormats.Any(available => available.Format == preferred)
                    );
                    if (imageFormat == Format.Undefined)
                        imageFormat = surfaceFormats.FirstOrDefault().Format;
                    if (imageFormat == Format.Undefined)
                        throw new InvalidOperationException("Surface somehow does not support any known image formats");
                    SwapchainImageFormat = imageFormat;
                    SwapchainColorSpace = surfaceFormats.First(e => e.Format == SwapchainImageFormat).ColorSpace;
                }
                // Choose the best present mode
                {
                    var presentMode = Array.Find(
                        PreferredPresentModes,
                        preferred => surfacePresentModes.Any(available => available == preferred)
                    );
                    SwapchainPresentMode = presentMode;
                }
                // Create the swapchain
                SwapchainExtent = surfaceCapabilities.CurrentExtent;
                Swapchain = Device.CreateSwapchainKhr(new SwapchainCreateInfoKhr(
                    surface: Surface,
                    imageFormat: SwapchainImageFormat,
                    imageExtent: SwapchainExtent,
                    preTransform: surfaceCapabilities.CurrentTransform,
                    presentMode: SwapchainPresentMode,
                    minImageCount: SurfaceCapabilities.MinImageCount,
                    imageArrayLayers: SurfaceCapabilities.MaxImageArrayLayers,
                    imageSharingMode: SharingMode.Exclusive,
                    imageColorSpace: SwapchainColorSpace
                ));
                SwapchainAttachmentImages = Swapchain.GetImages().Select(
                        e => new AttachmentImage(
                                e, SwapchainImageFormat,
                                SwapchainExtent,
                                new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)
                            )
                    ).ToArray();
                Swapchain.PrintDebug();
            }
            // Create semaphores & fences
            {
                // Image available semaphore
                ImageAvailableSemaphore = Device.CreateSemaphore();
                // Swapchain image rendering fences
                RenderToImageFences = new Fence[SwapchainAttachmentImages.Length];
                for (var i = 0; i < RenderToImageFences.Length; i++)
                    RenderToImageFences[i] = Device.CreateFence(new FenceCreateInfo(flags: FenceCreateFlags.Signaled));
            }
        }

        /// <summary>
        /// Print debug info about the object
        /// </summary>
        public void PrintDebug()
        {
            Debug.Info(string.Join("\n", DebugStrings), "Debug");
        }

        /// <summary>
        /// Acqure a new swapchain image and signal ImageAvailableSemaphore when done
        /// </summary>
        /// <returns></returns>
        public int NextSwapchainImage()
        {
            return Swapchain.AcquireNextImage(semaphore: ImageAvailableSemaphore);
        }

        /// <summary>
        /// Signal the element of RenderToImageFences corresponding to imageIndex after the given semaphore is signaled
        /// </summary>
        /// <returns></returns>
        public void SignalRenderToImageFence(Semaphore waitSemaphore, int imageIndex)
        {
            GraphicsQueueFamily.HighestPriority.Submit(
                    waitSemaphore: waitSemaphore,
                    waitDstStageMask: PipelineStages.BottomOfPipe,
                    commandBuffer: null,
                    signalSemaphore: null,
                    fence: RenderToImageFences[imageIndex]
                );
        }
    }
}
