using VulkanCore;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Runtime.InteropServices;

namespace WyvernFramework.Sprites
{
    public class SpriteEffect : InstanceRendererEffect
    {
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        private struct SpriteInstanceInfo
        {
            [FieldOffset(0)]
            public Vector3 Position;
            [FieldOffset(16)]
            public Vector2 Scale;
        }

        public const int MaxSets = 16;

        private ShaderModule VertexShader;
        private ShaderModule FragmentShader;
        private Sampler TextureSampler;
        private DescriptorPool DescriptorPool;
        private DescriptorSetLayout DescriptorSetLayout;
        private Dictionary<InstanceList, DescriptorSet> DescriptorSets { get; } = new Dictionary<InstanceList, DescriptorSet>();
        private VKBuffer<CameraUniformBlock> CameraUniform;
        private Dictionary<InstanceList, VKBuffer<SpriteInstanceInfo>> InstanceBuffers { get; }
                = new Dictionary<InstanceList, VKBuffer<SpriteInstanceInfo>>();
        private PipelineLayout PipelineLayout;
        private Pipeline Pipeline;
        private RenderPassObject RenderPass { get; }
        private Dictionary<VKImage, Framebuffer> Framebuffers { get; } = new Dictionary<VKImage, Framebuffer>();

        public SpriteEffect(Graphics graphics, RenderPassObject renderPass, ImageLayout initialLayout,
                Accesses initialAccess, PipelineStages initialStage)
            : base(
                    nameof(SpriteEffect), graphics, ImageLayout.ColorAttachmentOptimal,
                    Accesses.ColorAttachmentWrite, PipelineStages.ColorAttachmentOutput, initialLayout, initialAccess, initialStage
                )
        {
            RenderPass = renderPass;
        }

        public override void OnStart()
        {
            VertexShader = Graphics.Content.LoadShaderModule(Path.Combine("Sprites", "BasicSprites.vert.spv"));
            FragmentShader = Graphics.Content.LoadShaderModule(Path.Combine("Sprites", "BasicSprites.frag.spv"));
            TextureSampler = Graphics.Device.CreateSampler(new SamplerCreateInfo
            {
                MinFilter = Filter.Linear,
                MagFilter = Filter.Linear
            });
            DescriptorPool = Graphics.Device.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                    MaxSets, new DescriptorPoolSize[]
                    {
                        new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                        new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
                    },
                    DescriptorPoolCreateFlags.FreeDescriptorSet
                ));
            DescriptorSetLayout = Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                    new[]
                    {
                        new DescriptorSetLayoutBinding(
                                binding: 0,
                                descriptorType: DescriptorType.UniformBuffer,
                                descriptorCount: 1,
                                stageFlags: ShaderStages.Vertex
                            ),
                        new DescriptorSetLayoutBinding(
                                binding: 1,
                                descriptorType: DescriptorType.CombinedImageSampler,
                                descriptorCount: 1,
                                stageFlags: ShaderStages.Fragment
                            )
                    }
                ));
            PipelineLayout = Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                    setLayouts: new[] { DescriptorSetLayout }
                ));
            Pipeline = Graphics.Device.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                    layout: PipelineLayout,
                    renderPass: RenderPass.RenderPass,
                    subpass: 0,
                    stages: new[]
                    {
                        new PipelineShaderStageCreateInfo(ShaderStages.Vertex, VertexShader, "main"),
                        new PipelineShaderStageCreateInfo(ShaderStages.Fragment, FragmentShader, "main")
                    },
                    inputAssemblyState: new PipelineInputAssemblyStateCreateInfo(PrimitiveTopology.TriangleList),
                    vertexInputState: new PipelineVertexInputStateCreateInfo(
                            new VertexInputBindingDescription[]
                            {
                                new VertexInputBindingDescription(
                                        0,
                                        Interop.SizeOf<SpriteInstanceInfo>(),
                                        VertexInputRate.Instance
                                    )
                            },
                            new VertexInputAttributeDescription[]
                            {
                                new VertexInputAttributeDescription( // Position
                                        0, 0, Format.R32G32B32SFloat, 0
                                    ),
                                new VertexInputAttributeDescription( // Scale
                                        1, 0, Format.R32G32SFloat, 16
                                    )
                            }
                        ),
                    rasterizationState: new PipelineRasterizationStateCreateInfo(
                            polygonMode: PolygonMode.Fill,
                            cullMode: CullModes.Front,
                            frontFace: FrontFace.Clockwise,
                            lineWidth: 1f
                        ),
                    viewportState: new PipelineViewportStateCreateInfo(
                            new Viewport(0f, 0f, Graphics.Window.Size.X, Graphics.Window.Size.Y),
                            new Rect2D(0, 0, (int)Graphics.Window.Size.X, (int)Graphics.Window.Size.Y)
                        ),
                    multisampleState: new PipelineMultisampleStateCreateInfo(
                            rasterizationSamples: SampleCounts.Count1,
                            minSampleShading: 1
                        ),
                    colorBlendState: new PipelineColorBlendStateCreateInfo(
                            attachments: new[]
                            {
                                new PipelineColorBlendAttachmentState(
                                        blendEnable: true,
                                        srcColorBlendFactor: BlendFactor.One,
                                        dstColorBlendFactor: BlendFactor.Zero,
                                        colorBlendOp: BlendOp.Add,
                                        srcAlphaBlendFactor: BlendFactor.One,
                                        dstAlphaBlendFactor: BlendFactor.Zero,
                                        alphaBlendOp: BlendOp.Add,
                                        colorWriteMask: ColorComponents.All
                                    )
                            }
                        )
                ));
            CameraUniform = VKBuffer<CameraUniformBlock>.UniformBuffer(
                    $"{nameof(SpriteEffect)}.{nameof(CameraUniform)}",
                    Graphics,
                    1
                );
            SetCamera(Vector2.Zero, Graphics.Window.Size);
        }

        protected override void OnRegisterImage(VKImage image)
        {
            Framebuffers.Add(image, RenderPass.RenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    attachments: new[] { image.ImageView },
                    width: image.Extent.Width,
                    height: image.Extent.Height
                )));
        }

        protected override void OnUnregisterImage(VKImage image)
        {
            Framebuffers[image].Dispose();
            Framebuffers.Remove(image);
        }

        protected override void OnRecordCommandBuffer(VKImage image, CommandBuffer buffer)
        {
            buffer.Begin(new CommandBufferBeginInfo());
            var nonEmptyLists = InstanceLists.Values.Where(e => e.Count > 0);
            if (nonEmptyLists.Any())
            {
                buffer.CmdPipelineBarrier(
                        srcStageMask: InitialStage,
                        dstStageMask: PipelineStages.ColorAttachmentOutput,
                        imageMemoryBarriers: new ImageMemoryBarrier[]
                        {
                                new ImageMemoryBarrier(
                                        image: image.Image,
                                        subresourceRange: image.SubresourceRange,
                                        srcAccessMask: InitialAccess,
                                        dstAccessMask: Accesses.ColorAttachmentWrite,
                                        oldLayout: InitialLayout,
                                        newLayout: ImageLayout.ColorAttachmentOptimal
                                    )
                        }
                    );
                buffer.CmdBeginRenderPass(new RenderPassBeginInfo(
                        Framebuffers[image],
                        RenderPass.RenderPass,
                        new Rect2D(0, 0, image.Extent.Width, image.Extent.Height)
                    ));
                buffer.CmdBindPipeline(PipelineBindPoint.Graphics, Pipeline);
                foreach (var list in nonEmptyLists)
                {
                    if (!DescriptorSets.TryGetValue(list, out var descriptorSet))
                    {
                        throw new InvalidOperationException(
                                $"No {nameof(DescriptorSet)} corresponding to {nameof(InstanceList)} {list}"
                            );
                    }

                    buffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, PipelineLayout, descriptorSet);
                    buffer.CmdBindVertexBuffers(0, 1, new VulkanCore.Buffer[] { InstanceBuffers[list].Buffer }, new long[] { 0L });
                    buffer.CmdDraw(6, list.Count);
                }
                buffer.CmdEndRenderPass();
            }
            buffer.End();
        }

        public override void OnDraw(Semaphore start, VKImage image)
        {
            if (AnyUpdatedInstanceLists)
            {
                RecreateDescriptorSets();
                RecreateInstanceBuffers();
                Refresh();
                ClearUpdates();
            }
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.ColorAttachmentOutput, GetCommandBuffer(image), FinishedSemaphore
                );
        }

        private void RecreateDescriptorSets()
        {
            Graphics.Device.WaitIdle();
            // Clean up
            foreach (var set in DescriptorSets.Values)
                set.Dispose();
            DescriptorSets.Clear();
            // Create sets
            foreach (var keyList in InstanceLists)
            {
                var (texture, rectangle) = ((Texture2D, Rect2D))keyList.Key;
                var list = keyList.Value;
                var set = DescriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                        1, DescriptorSetLayout
                    ))[0];
                DescriptorPool.UpdateSets(
                        new WriteDescriptorSet[]
                        {
                            new WriteDescriptorSet(
                                    set, 0, 0, 1, DescriptorType.UniformBuffer,
                                    bufferInfo: new DescriptorBufferInfo[]
                                    {
                                        new DescriptorBufferInfo(CameraUniform.Buffer)
                                    }
                                ),
                            new WriteDescriptorSet(
                                    set, 1, 0, 1, DescriptorType.CombinedImageSampler,
                                    imageInfo: new DescriptorImageInfo[]
                                    {
                                        new DescriptorImageInfo(
                                                TextureSampler,
                                                texture.Image.ImageView,
                                                ImageLayout.ShaderReadOnlyOptimal
                                            )
                                    }
                                )
                        }
                    );
                DescriptorSets.Add(list, set);
            }
        }

        private void RecreateInstanceBuffers()
        {
            // Create sets
            foreach (var keyList in InstanceLists)
            {
                var list = keyList.Value;
                VKBuffer<SpriteInstanceInfo> buffer;
                if (!InstanceBuffers.TryGetValue(list, out buffer))
                {
                    buffer = VKBuffer<SpriteInstanceInfo>.VertexBuffer(
                            $"{nameof(SpriteEffect)} instance buffer for list {list}",
                            Graphics,
                            InstanceList.MaxInstances
                        );
                    InstanceBuffers.Add(list, buffer);
                }
                using (var stagingBuffer = VKBuffer<SpriteInstanceInfo>.StagingBuffer(
                        $"{nameof(SpriteEffect)}.{nameof(RecreateInstanceBuffers)} staging buffer",
                        Graphics,
                        list.Count
                    ))
                {
                    unsafe
                    {
                        var ptr = stagingBuffer.Map(0, list.Count);
                        foreach (SpriteInstance inst in list.AllInstances)
                        {
                            *ptr = new SpriteInstanceInfo
                            {
                                Position = inst.Position,
                                Scale = inst.Scale
                            };
                            ptr++;
                        }
                        stagingBuffer.Unmap();

                    }
                    using (var stagingFence = Graphics.Device.CreateFence(new FenceCreateInfo()))
                    {
                        using (var stagingCommands = Graphics.TransferQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, 1)[0])
                        {
                            stagingCommands.Begin();
                            stagingCommands.CmdPipelineBarrier(
                                    PipelineStages.TopOfPipe, PipelineStages.Transfer,
                                    bufferMemoryBarriers: new BufferMemoryBarrier[]
                                    {
                                        new BufferMemoryBarrier(
                                                buffer.Buffer,
                                                Accesses.None, Accesses.TransferWrite,
                                                0L, stagingBuffer.Size
                                            )
                                    }
                                );
                            stagingCommands.CmdCopyBuffer(
                                    stagingBuffer.Buffer,
                                    buffer.Buffer,
                                    new BufferCopy[] { new BufferCopy(stagingBuffer.Size) }
                                );
                            stagingCommands.CmdPipelineBarrier(
                                    PipelineStages.Transfer, PipelineStages.VertexInput,
                                    bufferMemoryBarriers: new BufferMemoryBarrier[]
                                    {
                                        new BufferMemoryBarrier(
                                                buffer.Buffer,
                                                Accesses.TransferWrite, Accesses.VertexAttributeRead,
                                                0L, stagingBuffer.Size
                                            )
                                    }
                                );
                            stagingCommands.End();

                            Graphics.TransferQueueFamily.HighestPriority.Submit(
                                    new SubmitInfo(
                                            commandBuffers: new CommandBuffer[] { stagingCommands }
                                        ),
                                    stagingFence
                                );
                            stagingFence.Wait();
                        }
                    }
                }
            }
        }

        public void SetCamera(Matrix4x4 view, Matrix4x4 projection)
        {
            unsafe
            {
                var ptr = CameraUniform.Map(0, 1);
                *ptr = new CameraUniformBlock
                {
                    View = view,
                    Projection = projection
                };
                CameraUniform.Unmap();
            }
        }

        public void SetCamera(Vector2 viewCenter, Vector2 viewSize, float minZ = -1f, float maxZ = 1f)
        {
            unsafe
            {
                SetCamera(
                    Matrix4x4.CreateLookAt(new Vector3(viewCenter, maxZ), new Vector3(viewCenter, 0f), Vector3.UnitY),
                    Matrix4x4.CreateOrthographic(viewSize.X, viewSize.Y, maxZ, minZ)
                );
            }
        }
    }
}
