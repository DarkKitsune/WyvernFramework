using System;
using VulkanCore;

namespace WyvernFramework
{
    /// <summary>
    /// Wraps a vulkan buffer and its backing memory
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class VKBuffer<T> : IDebug, IDisposable
        where T : unmanaged
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the object
        /// </summary>
        public string Description => "A Vulkan buffer";

        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// Whether the buffer is mapped to application address space
        /// </summary>
        public bool Mapped { get; private set; }

        /// <summary>
        /// The Graphics object the buffer is associated with
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// The Vulkan buffer
        /// </summary>
        public VulkanCore.Buffer Buffer { get; }

        /// <summary>
        /// Buffer element count
        /// </summary>
        public long Count { get; }

        /// <summary>
        /// Buffer size in bytes
        /// </summary>
        public long Size { get; }

        /// <summary>
        /// The allowed usages of the buffer
        /// </summary>
        public BufferUsages Usages { get; }

        /// <summary>
        /// The device memory the buffer is backed by
        /// </summary>
        public DeviceMemory DeviceMemory { get; }

        public VKBuffer(
                string name, Graphics graphics, long count, BufferUsages usages, MemoryProperties memoryProperties,
                BufferCreateFlags flags = BufferCreateFlags.None, SharingMode sharingMode = SharingMode.Exclusive,
                int[] queueFamilyIndices = null
            )
        {
            Name = name;
            Graphics = graphics;
            Count = count;
            Size = count * Interop.SizeOf<T>();
            Usages = usages;
            Buffer = Graphics.Device.CreateBuffer(new BufferCreateInfo(
                    size: Size,
                    usages: usages,
                    flags: flags,
                    sharingMode: sharingMode,
                    queueFamilyIndices: queueFamilyIndices
                ));
            var reqs = Buffer.GetMemoryRequirements();
            DeviceMemory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(
                    reqs.Size,
                    graphics.GetMemoryTypeIndex(reqs.MemoryTypeBits, memoryProperties)
                ));
            Buffer.BindMemory(DeviceMemory);
        }

        ~VKBuffer()
        {
            Dispose();
        }

        /// <summary>
        /// Create a buffer for staging
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VKBuffer<T> StagingBuffer(string name, Graphics graphics, long count)
        {
            return new VKBuffer<T>(
                    name: name,
                    graphics: graphics,
                    count: count,
                    usages: BufferUsages.TransferSrc,
                    memoryProperties: MemoryProperties.HostVisible | MemoryProperties.HostCoherent
                );
        }

        /// <summary>
        /// Create a uniform buffer
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VKBuffer<T> UniformBuffer(string name, Graphics graphics, long count, bool transferSrc = false)
        {
            return new VKBuffer<T>(
                    name: name,
                    graphics: graphics,
                    count: count,
                    usages: BufferUsages.UniformBuffer
                        | (transferSrc ? BufferUsages.TransferSrc : 0),
                    memoryProperties: MemoryProperties.HostVisible | MemoryProperties.HostCoherent
                );
        }

        /// <summary>
        /// Create a vertex buffer
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VKBuffer<T> VertexBuffer(string name, Graphics graphics, long count, bool transferSrc = false)
        {
            return new VKBuffer<T>(
                    name: name,
                    graphics: graphics,
                    count: count,
                    usages: BufferUsages.VertexBuffer
                        | BufferUsages.TransferDst
                        | (transferSrc ? BufferUsages.TransferSrc : 0),
                    memoryProperties: MemoryProperties.DeviceLocal
                );
        }

        /// <summary>
        /// Create an index buffer
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VKBuffer<T> IndexBuffer(string name, Graphics graphics, long count, bool transferSrc = false)
        {
            return new VKBuffer<T>(
                    name: name,
                    graphics: graphics,
                    count: count,
                    usages: BufferUsages.IndexBuffer
                        | BufferUsages.TransferDst
                        | (transferSrc ? BufferUsages.TransferSrc : 0),
                    memoryProperties: MemoryProperties.DeviceLocal
                );
        }

        /// <summary>
        /// Create a storage buffer
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static VKBuffer<T> StorageBuffer(string name, Graphics graphics, long count, bool transferSrc = false)
        {
            return new VKBuffer<T>(
                    name: name,
                    graphics: graphics,
                    count: count,
                    usages: BufferUsages.VertexBuffer
                        | BufferUsages.StorageBuffer
                        | BufferUsages.TransferDst
                        | (transferSrc ? BufferUsages.TransferSrc : 0),
                    memoryProperties: MemoryProperties.DeviceLocal
                );
        }

        /// <summary>
        /// Map part of the buffer memory to application address space
        /// </summary>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public unsafe T* Map(long index, long count)
        {
            if (Mapped)
                throw new InvalidOperationException("Buffer is already mapped");
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "index must be >= 0");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "count must be >= 0");
            if (index < 0 || index >= Count)
            {
                throw new IndexOutOfRangeException(
                        $"{nameof(index)} ({index}) was outside of the buffer's range (0 - {Count - 1})"
                    );
            }
            if (index + count > Count)
            {
                throw new IndexOutOfRangeException(
                        $"{nameof(index)}+{nameof(count)} ({index + count}) was outside of the buffer's range (0 = {Count})"
                    );
            }
            Mapped = true;
            var size = Interop.SizeOf<T>();
            return (T*)DeviceMemory.Map(index * size, count * size);
        }

        /// <summary>
        /// Unmap the buffer memory from application address space
        /// </summary>
        public void Unmap()
        {
            if (!Mapped)
                throw new InvalidOperationException("Buffer is not mapped");
            DeviceMemory.Unmap();
            Mapped = false;
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            Buffer.Dispose();
            DeviceMemory.Dispose();
        }
    }
}
