using VulkanCore;
using WyvernFramework;
using System.IO;
using System.Collections.Generic;

namespace Demos.GraphicalEffects
{
    public class TriangleTestEffect : GraphicalEffect
    {
        private Texture2D Texture;
        private ShaderModule VertexShader;
        private ShaderModule FragmentShader;
        private Sampler TextureSampler;
        private DescriptorPool DescriptorPool;
        private DescriptorSetLayout DescriptorSetLayout;
        private DescriptorSet TextureDescriptorSet;
        private PipelineLayout PipelineLayout;
        private Pipeline Pipeline;
        private RenderPassObject RenderPass { get; }
        private Dictionary<VKImage, Framebuffer> Framebuffers { get; } = new Dictionary<VKImage, Framebuffer>();

        public TriangleTestEffect(Graphics graphics, RenderPassObject renderPass, Texture2D texture, ImageLayout initialLayout,
                Accesses initialAccess, PipelineStages initialStage)
            : base(
                    nameof(TriangleTestEffect), graphics, ImageLayout.ColorAttachmentOptimal,
                    Accesses.ColorAttachmentWrite, PipelineStages.ColorAttachmentOutput, initialLayout, initialAccess, initialStage
                )
        {
            RenderPass = renderPass;
            Texture = texture;
        }

        public override void OnStart()
        {
            // Create triangle pipeline
            {
                // Load shaders
                {
                    var vertPath = Path.Combine("..", "..", "..", "Content", "Shader.vert.spv");
                    var fragPath = Path.Combine("..", "..", "..", "Content", "Shader.frag.spv");
                    VertexShader = Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(
                            File.ReadAllBytes(vertPath)
                        ));
                    FragmentShader = Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(
                            File.ReadAllBytes(fragPath)
                        ));
                }
                // Create sampler
                {
                    TextureSampler = Graphics.Device.CreateSampler(new SamplerCreateInfo
                    {
                        MinFilter = Filter.Linear,
                        MagFilter = Filter.Linear
                    });
                }
                // Create descriptor set layout
                {
                    DescriptorSetLayout = Graphics.Device.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(
                            new[]
                            {
                                new DescriptorSetLayoutBinding(
                                        binding: 0,
                                        descriptorType: DescriptorType.CombinedImageSampler,
                                        descriptorCount: 1,
                                        stageFlags: ShaderStages.Fragment
                                    )
                            }
                        ));
                }
                // Create descriptor pool
                {
                    DescriptorPool = Graphics.Device.CreateDescriptorPool(new DescriptorPoolCreateInfo(
                            1,
                            new[]
                            {
                                new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1)
                            }
                        ));
                }
                // Create descriptor set
                {
                    TextureDescriptorSet = DescriptorPool.AllocateSets(new DescriptorSetAllocateInfo(
                            1, DescriptorSetLayout
                        ))[0];
                    var writes = new[]
                    {
                        new WriteDescriptorSet(
                                TextureDescriptorSet, 0, 0, 1, DescriptorType.CombinedImageSampler,
                                imageInfo: new[]
                                {
                                    new DescriptorImageInfo(
                                            TextureSampler,
                                            Texture.Image.ImageView,
                                            ImageLayout.ShaderReadOnlyOptimal
                                        )
                                }
                            )
                    };
                    DescriptorPool.UpdateSets(writes);
                }
                // Create pipeline layout
                {
                    PipelineLayout = Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo(
                            setLayouts: new[] { DescriptorSetLayout }
                        ));
                }
                // Create graphics pipeline
                {
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
                            vertexInputState: new PipelineVertexInputStateCreateInfo(),
                            rasterizationState: new PipelineRasterizationStateCreateInfo(
                                    polygonMode: PolygonMode.Fill,
                                    cullMode: CullModes.Back,
                                    frontFace: FrontFace.CounterClockwise,
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
                }
            }
        }

        protected override CommandBuffer OnRegisterImage(VKImage image)
        {
            // Create image view and framebuffer
            {
                Framebuffers.Add(image, RenderPass.RenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                        attachments: new[] { image.ImageView },
                        width: image.Extent.Width,
                        height: image.Extent.Height
                    )));
            }
            // Create and record command buffer
            {
                // Create command buffer
                var buffer = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, 1)[0];
                // Begin recording
                buffer.Begin(new CommandBufferBeginInfo());
                // Write commands
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
                buffer.CmdBindDescriptorSet(PipelineBindPoint.Graphics, PipelineLayout, TextureDescriptorSet);
                buffer.CmdDraw(3);
                buffer.CmdEndRenderPass();
                // Finish recording
                buffer.End();
                // Return buffer
                return buffer;
            }
        }

        protected override void OnUnregisterImage(VKImage image)
        {
            Framebuffers[image].Dispose();
            Framebuffers.Remove(image);
        }

        public override void OnDraw(Semaphore start, VKImage image)
        {
            // Submit the command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.ColorAttachmentOutput, CommandBuffers[image], FinishedSemaphore
                );
        }
    }
}
