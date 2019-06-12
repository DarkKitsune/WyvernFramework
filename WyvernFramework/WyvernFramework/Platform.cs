using System;
using VulkanCore;

namespace WyvernFramework
{
    public static class Platform
    {
        public enum PlatformType
        {
            Win32, Android, MacOS
        }

        /// <summary>
        /// Get which platform the app is running on
        /// </summary>
        public static PlatformType Type
        {
            get
            {
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32NT:
                        return PlatformType.Win32;
                    default:
                        // TODO: implement other platform types
                        throw new NotImplementedException();
                }
            }
        }
    }
}
