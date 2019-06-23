﻿using VulkanCore;
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
        [StructLayout(LayoutKind.Explicit, Size = 56)]
        private struct SpriteInstanceInfo
        {
            [FieldOffset(0)]
            public float Time;
            [FieldOffset(16)]
            public Vector3 Position;
            [FieldOffset(32)]
            public Vector3 Velocity;
            [FieldOffset(48)]
            public Vector2 Scale;
            [FieldOffset(56)]
            public int ListIndex;
            [FieldOffset(64)]
            public Vector4 Rectangle;
        }

        public const int MaxSets = 32;

        private ShaderModule ComputeShader;
        private ShaderModule VertexShader;
        private ShaderModule FragmentShader;
        private Sampler TextureSampler;
        private DescriptorPool GraphicsDescriptorPool;
        private DescriptorPool ComputeDescriptorPool;
        private DescriptorSetLayout GraphicsDescriptorSetLayout;
        private DescriptorSetLayout ComputeDescriptorSetLayout;
        private Dictionary<InstanceList, DescriptorSet> GraphicsDescriptorSets { get; } = new Dictionary<InstanceList, DescriptorSet>();
        private Dictionary<InstanceList, DescriptorSet> ComputeDescriptorSets { get; } = new Dictionary<InstanceList, DescriptorSet>();
        private VKBuffer<CameraUniformBlock> CameraUniform;
        private VKBuffer<float> TimeUniform;
        private Dictionary<InstanceList, VKBuffer<SpriteInstanceInfo>> InstanceBuffers { get; }
                = new Dictionary<InstanceList, VKBuffer<SpriteInstanceInfo>>();
        private PipelineLayout GraphicsPipelineLayout;
        private PipelineLayout ComputePipelineLayout;
        private Pipeline GraphicsPipeline;
        private Pipeline ComputePipeline;
        private RenderPassObject RenderPass { get; }
        private Dictionary<VKImage, Framebuffer> Framebuffers { get; } = new Dictionary<VKImage, Framebuffer>();
        private Dictionary<VKImage, CommandBuffer> ComputeCommandBuffers { get; } = new Dictionary<VKImage, CommandBuffer>();
        private Semaphore ComputeSemaphore;

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
            ComputeShader = Graphics.Content.LoadShaderModule(Path.Combine("Sprites", "BasicSprites.comp.spv"));
            VertexShader = Graphics.Content.LoadShaderModule(Path.Combine("Sprites", "BasicSprites.vert.spv"));
            FragmentShader = Graphics.Content.LoadShaderModule(Path.Combine("Sprites", "BasicSprites.frag.spv"));
            TextureSampler = Graphics.Device.CreateSampler(new SamplerCreateInfo
            {
                MinFilter = Filter.Linear,
                MagFilter = Filter.Linear
            });
            GraphicsDescriptorPool = Graphics.Device.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                    MaxSets, new DescriptorPoolSize[]
                    {
                        new DescriptorPoolSize(DescriptorType.UniformBuffer, 1),
                        new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1),
                        new DescriptorPoolSize(DescriptorType.UniformBuffer, 1)
                    },
                    DescriptorPoolCreateFlags.FreeDescriptorSet
                ));
            ComputeDescriptorPool = Graphics.Device.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                    MaxSets, new DescriptorPoolSize[]
                    {
                        new DescriptorPoolSize(DescriptorType.StorageBuffer, 1),
                        new DescriptorPoolSize(DescriptorType.UniformBuffer, 1)
                    },
                    DescriptorPoolCreateFlags.FreeDescriptorSet
                ));
            GraphicsDescriptorSetLayout = Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
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
                            ),
                        new DescriptorSetLayoutBinding(
                                binding: 2,
                                descriptorType: DescriptorType.UniformBuffer,
                                descriptorCount: 1,
                                stageFlags: ShaderStages.Vertex
                            )
                    }
                ));
            ComputeDescriptorSetLayout = Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                    new[]
                    {
                        new DescriptorSetLayoutBinding(
                                binding: 0,
                                descriptorType: DescriptorType.StorageBuffer,
                                descriptorCount: 1,
                                stageFlags: ShaderStages.Compute
                            ),
                        new DescriptorSetLayoutBinding(
                                binding: 1,
                                descriptorType: DescriptorType.UniformBuffer,
                                descriptorCount: 1,
                                stageFlags: ShaderStages.Compute
                            )
                    }
                ));
            GraphicsPipelineLayout = Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                    setLayouts: new[] { GraphicsDescriptorSetLayout }
                ));
            ComputePipelineLayout = Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                    setLayouts: new[] { ComputeDescriptorSetLayout }
                ));
            GraphicsPipeline = Graphics.Device.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                    layout: GraphicsPipelineLayout,
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
                                new VertexInputAttributeDescription( // Time
                                        0, 0, Format.R32G32B32SFloat, 0
                                    ),
                                new VertexInputAttributeDescription( // Position
                                        1, 0, Format.R32G32B32SFloat, 16
                                    ),
                                new VertexInputAttributeDescription( // Velocity
                                        2, 0, Format.R32G32SFloat, 32
                                    ),
                                new VertexInputAttributeDescription( // Scale
                                        3, 0, Format.R32G32SFloat, 48
                                    ),
                                new VertexInputAttributeDescription( // ListIndex
                                        4, 0, Format.R32SInt, 56
                                    ),
                                new VertexInputAttributeDescription( // ListIndex
                                        5, 0, Format.R32G32B32A32SFloat, 64
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
            ComputePipeline = Graphics.Device.CreateComputePipeline(new ComputePipelineCreateInfo(
                    stage: new PipelineShaderStageCreateInfo(ShaderStages.Compute, ComputeShader, "main"),
                    layout: ComputePipelineLayout,
                    flags: PipelineCreateFlags.None
                ));
            CameraUniform = VKBuffer<CameraUniformBlock>.UniformBuffer(
                    $"{nameof(SpriteEffect)}.{nameof(CameraUniform)}",
                    Graphics,
                    1
                );
            TimeUniform = VKBuffer<float>.UniformBuffer(
                    $"{nameof(SpriteEffect)}.{nameof(TimeUniform)}",
                    Graphics,
                    MaxSets
                );
            ComputeSemaphore = Graphics.Device.CreateSemaphore();
            SetCamera(Vector2.Zero, Graphics.Window.Size);
        }

        protected override void OnRegisterImage(VKImage image)
        {
            Framebuffers.Add(image, RenderPass.RenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                    attachments: new[] { image.ImageView },
                    width: image.Extent.Width,
                    height: image.Extent.Height
                )));
            ComputeCommandBuffers.Add(
                    image,
                    Graphics.ComputeQueueFamily.CreateCommandBuffers(
                            CommandBufferLevel.Primary,
                            1
                        )[0]
                );
        }

        protected override void OnUnregisterImage(VKImage image)
        {
            Framebuffers[image].Dispose();
            Framebuffers.Remove(image);
            ComputeCommandBuffers[image].Dispose();
            ComputeCommandBuffers.Remove(image);
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
                buffer.CmdBindPipeline(PipelineBindPoint.Graphics, GraphicsPipeline);
                foreach (var list in nonEmptyLists)
                {
                    if (!GraphicsDescriptorSets.TryGetValue(list, out var descriptorSet))
                    {
                        throw new InvalidOperationException(
                                $"No graphics {nameof(DescriptorSet)} corresponding to {nameof(InstanceList)} {list}"
                            );
                    }

                    buffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, GraphicsPipelineLayout, descriptorSet);
                    buffer.CmdBindVertexBuffers(0, 1, new VulkanCore.Buffer[] { InstanceBuffers[list].Buffer }, new long[] { 0L });
                    buffer.CmdDraw(6, list.Count);
                }
                buffer.CmdEndRenderPass();
            }
            buffer.End();

            buffer = ComputeCommandBuffers[image];
            buffer.Begin(new CommandBufferBeginInfo());
            if (nonEmptyLists.Any())
            {
                buffer.CmdBindPipeline(PipelineBindPoint.Compute, ComputePipeline);
                foreach (var list in nonEmptyLists)
                {
                    buffer.CmdPipelineBarrier(
                            PipelineStages.VertexInput, PipelineStages.ComputeShader,
                            bufferMemoryBarriers: new BufferMemoryBarrier[]
                            {
                                new BufferMemoryBarrier(
                                        InstanceBuffers[list].Buffer,
                                        Accesses.VertexAttributeRead, Accesses.ShaderWrite,
                                        0L, list.Count
                                    )
                            }
                        );
                    if (!ComputeDescriptorSets.TryGetValue(list, out var descriptorSet))
                    {
                        throw new InvalidOperationException(
                                $"No compute {nameof(DescriptorSet)} corresponding to {nameof(InstanceList)} {list}"
                            );
                    }
                    buffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, ComputePipelineLayout, descriptorSet);
                    buffer.CmdDispatch((int)MathF.Ceiling(list.Count / 256f), 1, 1);
                    buffer.CmdPipelineBarrier(
                            PipelineStages.ComputeShader, PipelineStages.VertexInput,
                            bufferMemoryBarriers: new BufferMemoryBarrier[]
                            {
                                new BufferMemoryBarrier(
                                        InstanceBuffers[list].Buffer,
                                        Accesses.ShaderWrite, Accesses.VertexAttributeRead,
                                        0L, list.Count
                                    )
                            }
                        );
                }
            }
            buffer.End();
        }

        public override void OnDraw(Semaphore start, VKImage image)
        {
            if (AnyUpdatedInstanceLists)
            {
                UpdateLists();
                RecreateInstanceBuffers();
                RecreateDescriptorSets();
                Refresh();
            }
            unsafe
            {
                var ptr = TimeUniform.Map(0, MaxSets);
                var i = 0;
                foreach (var list in InstanceLists.Values.Where(e => e.Count > 0))
                {
                    *(ptr++) = (float)list.TimeSinceLastUpdate;
                }
                TimeUniform.Unmap();
            }
            var graphicsCommandBuffer = GetCommandBuffer(image);
            Graphics.ComputeQueueFamily.First.Submit(
                   start, PipelineStages.ComputeShader, ComputeCommandBuffers[image], ComputeSemaphore
               );
            Graphics.GraphicsQueueFamily.First.Submit(
                    ComputeSemaphore, PipelineStages.ColorAttachmentOutput, graphicsCommandBuffer, FinishedSemaphore
                );
        }

        private void RecreateDescriptorSets()
        {
            Graphics.Device.WaitIdle();
            // Clean up
            foreach (var set in GraphicsDescriptorSets.Values)
                set.Dispose();
            GraphicsDescriptorSets.Clear();
            foreach (var set in ComputeDescriptorSets.Values)
                set.Dispose();
            ComputeDescriptorSets.Clear();
            // Create sets
            foreach (var keyList in InstanceLists)
            {
                var texture = (Texture2D)keyList.Key;
                var list = keyList.Value;
                // Graphics set
                var set = GraphicsDescriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                        1, GraphicsDescriptorSetLayout
                    ))[0];
                GraphicsDescriptorPool.UpdateSets(
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
                                ),
                            new WriteDescriptorSet(
                                    set, 2, 0, 1, DescriptorType.UniformBuffer,
                                    bufferInfo: new DescriptorBufferInfo[]
                                    {
                                        new DescriptorBufferInfo(TimeUniform.Buffer)
                                    }
                                )
                        }
                    );
                GraphicsDescriptorSets.Add(list, set);
                // Compute set
                set = ComputeDescriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                        1, ComputeDescriptorSetLayout
                    ))[0];
                ComputeDescriptorPool.UpdateSets(
                        new WriteDescriptorSet[]
                        {
                            new WriteDescriptorSet(
                                    set, 0, 0, 1, DescriptorType.StorageBuffer,
                                    bufferInfo: new DescriptorBufferInfo[]
                                    {
                                        new DescriptorBufferInfo(InstanceBuffers[list].Buffer)
                                    }
                                ),
                            new WriteDescriptorSet(
                                    set, 1, 0, 1, DescriptorType.UniformBuffer,
                                    bufferInfo: new DescriptorBufferInfo[]
                                    {
                                        new DescriptorBufferInfo(TimeUniform.Buffer)
                                    }
                                )
                        }
                    );
                ComputeDescriptorSets.Add(list, set);
            }
        }

        private void RecreateInstanceBuffers()
        {
            // Create sets
            var listInd = 0;
            foreach (var keyList in InstanceLists.Where(e => e.Value.Count > 0))
            {
                var list = keyList.Value;
                VKBuffer<SpriteInstanceInfo> buffer;
                if (!InstanceBuffers.TryGetValue(list, out buffer))
                {
                    buffer = VKBuffer<SpriteInstanceInfo>.StorageBuffer(
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
                                Time = (float)inst.LastStoreTime,
                                Position = inst.StoredPosition,
                                Velocity = inst.Velocity,
                                Scale = inst.Scale,
                                ListIndex = listInd,
                                Rectangle = inst.Rectangle
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
                listInd++;
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
