using VulkanCore;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WyvernFramework
{
    public class Texture2D : IDebug, IDisposable
    {
        /// <summary>
        /// The name of the object
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// A description of the object
        /// </summary>
        public string Description => "A Vulkan image meant to be used as a texture";

        /// <summary>
        /// Whether the object has been disposed
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        /// The Graphics object associated with the texture
        /// </summary>
        public Graphics Graphics { get; }

        /// <summary>
        /// The Vulkan image
        /// </summary>
        public VKImage Image { get; }

        /// <summary>
        /// The device memory backing the image
        /// </summary>
        public DeviceMemory DeviceMemory { get; }

        public Texture2D(
                string name, Graphics graphics, TextureData2D data
            )
        {
            Name = name;
            Graphics = graphics;

            // Create image
            var image = Graphics.Device.CreateImage(new ImageCreateInfo
            {
                ImageType = ImageType.Image2D,
                Format = data.Format,
                MipLevels = data.MipMaps.Length,
                ArrayLayers = 1,
                Samples = SampleCounts.Count1,
                Tiling = ImageTiling.Optimal,
                SharingMode = SharingMode.Exclusive,
                InitialLayout = ImageLayout.Undefined,
                Extent = new Extent3D(data.MipMaps[0].Extent.Width, data.MipMaps[0].Extent.Height, 1),
                Usage = ImageUsages.Sampled | ImageUsages.TransferDst
            });
            Image = new VKImage(
                    image, data.Format, data.MipMaps[0].Extent,
                    new ImageSubresourceRange(ImageAspects.Color, 0, data.MipMaps.Length, 0, 1
                ));
            var memReq = Image.Image.GetMemoryRequirements();
            DeviceMemory = Graphics.Device.AllocateMemory(new MemoryAllocateInfo(
                    allocationSize: memReq.Size,
                    memoryTypeIndex: Graphics.GetMemoryTypeIndex(memReq.MemoryTypeBits, MemoryProperties.DeviceLocal)
                ));
            Image.Image.BindMemory(DeviceMemory);

            // Copy data to staging buffer
            var staging = VKBuffer<byte>.StagingBuffer($"{nameof(Texture2D)} staging buffer", Graphics, data.Size);
            unsafe
            {
                var dest = staging.Map(0, data.Size);
                foreach (var mip in data.MipMaps)
                {
                    fixed (byte* src = mip.Data)
                    {
                        System.Buffer.MemoryCopy(src, dest, mip.Size, mip.Size);
                        dest += mip.Size;
                    }
                }
                staging.Unmap();
            }

            // Create copy regions
            var regions = new BufferImageCopy[data.MipMaps.Length];
            var offset = 0L;
            for (var i = 0; i < regions.Length; i++)
            {
                regions[i] = new BufferImageCopy
                {
                    ImageSubresource = new ImageSubresourceLayers(ImageAspects.Color, i, 0, 1),
                    ImageExtent = new Extent3D(data.MipMaps[0].Extent.Width, data.MipMaps[0].Extent.Height, 1),
                    BufferOffset = offset
                };
                offset += data.MipMaps[i].Size;
            }

            // Create command buffer
            var range = new ImageSubresourceRange(ImageAspects.Color, 0, data.MipMaps.Length, 0, 1);
            var buffer = Graphics.TransferQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, 1)[0];
            buffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
            buffer.CmdPipelineBarrier(
                    PipelineStages.TopOfPipe, PipelineStages.Transfer,
                    imageMemoryBarriers: new ImageMemoryBarrier[]
                    {
                        new ImageMemoryBarrier(
                                Image.Image, range,
                                Accesses.None, Accesses.TransferWrite,
                                ImageLayout.Undefined, ImageLayout.TransferDstOptimal
                            )
                    }
                );
            buffer.CmdCopyBufferToImage(staging.Buffer, Image.Image, ImageLayout.TransferDstOptimal, regions);
            buffer.CmdPipelineBarrier(
                    PipelineStages.Transfer, PipelineStages.FragmentShader,
                    imageMemoryBarriers: new ImageMemoryBarrier[]
                    {
                        new ImageMemoryBarrier(
                                Image.Image, range,
                                Accesses.TransferWrite, Accesses.ShaderRead,
                                ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal
                            )
                    }
                );
            buffer.End();

            // Submit the buffer
            var fence = Graphics.Device.CreateFence(new FenceCreateInfo());
            Graphics.GraphicsQueueFamily.HighestPriority.Submit(
                    new SubmitInfo(
                            commandBuffers: new CommandBuffer[] { buffer }
                        ),
                    fence
                );
            fence.Wait();

            // Clean up
            fence.Dispose();
            staging.Dispose();
        }

        /// <summary>
        /// Dispose the object
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                return;
            Disposed = true;
            Image.Dispose();
            DeviceMemory.Dispose();
        }

        /// <summary>
        /// Create a Texture2D from a bitmap
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static Texture2D FromBitmap(string name, Graphics graphics, Bitmap image)
        {
            var data = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb
                );
            var mipmap = new MipMap2D(
                    data.Scan0,
                    data.Stride * data.Height,
                    new Extent2D(image.Width, image.Height)
                );
            image.UnlockBits(data);
            return new Texture2D(name, graphics, new TextureData2D(new[] { mipmap }));
        }

        /// <summary>
        /// Create a Texture2D from a bitmap in a stream
        /// </summary>
        /// <param name="name"></param>
        /// <param name="graphics"></param>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Texture2D FromStream(string name, Graphics graphics, Stream stream)
        {
            using (var bmp = (Bitmap)System.Drawing.Image.FromStream(stream))
                return FromBitmap(name, graphics, bmp);
        }

        /// <summary>
        /// Create a Texture2D from a bitmap file
        /// </summary>
        /// <param name="name"></param>
        /// <param name="graphics"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Texture2D FromFile(string name, Graphics graphics, string path)
        {
            using (var stream = File.OpenRead(path))
                return FromStream(name, graphics, stream);
        }
    }
}
