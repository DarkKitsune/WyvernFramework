using System;
using VkGLFW3;
using WyvernFramework;

namespace Demos
{
    static class Program
    {
        static void Main(string[] args)
        {
            WyvernWindow.Init();
            if (!VkGlfw.VulkanSupported)
                throw new PlatformNotSupportedException("Vulkan unsupported on this machine!");

            using (var window = new AppWindow())
                window.Start();

            WyvernWindow.Terminate();
        }
    }
}
