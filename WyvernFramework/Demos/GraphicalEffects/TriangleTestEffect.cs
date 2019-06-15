using VulkanCore;
using WyvernFramework;
using System.IO;
using System.Collections.Generic;

namespace Demos.GraphicalEffects
{
    public class TriangleTestEffect : GraphicalEffect
    {
        /// <summary>
        /// The triangle vertex shader
        /// </summary>
        private ShaderModule VertexShader;

        /// <summary>
        /// The triangle fragment shader
        /// </summary>
        private ShaderModule FragmentShader;

        /// <summary>
        /// The triangle graphics pipeline
        /// </summary>
        private Pipeline Pipeline;

        /// <summary>
        /// Framebuffers for registered images
        /// </summary>
        private Dictionary<AttachmentImage, Framebuffer> Framebuffers { get; } = new Dictionary<AttachmentImage, Framebuffer>();

        public TriangleTestEffect(Graphics graphics, RenderPassObject renderPass, ImageLayout initialLayout,
                Accesses initialAccess, PipelineStages initialStage)
            : base(
                    nameof(TriangleTestEffect), graphics, renderPass, ImageLayout.ColorAttachmentOptimal,
                    Accesses.ColorAttachmentWrite, PipelineStages.ColorAttachmentOutput, initialLayout, initialAccess, initialStage
                )
        {
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
                // Create graphics pipeline
                {
                    Pipeline = Graphics.Device.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                            layout: Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo()),
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
                            multisampleState: new PipelineMultisampleStateCreateInfo(),
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

        protected override CommandBuffer OnRegisterImage(AttachmentImage image)
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
                buffer.CmdDraw(3);
                // Finish recording
                buffer.End();
                // Return buffer
                return buffer;
            }
        }

        protected override void OnUnregisterImage(AttachmentImage image)
        {
            Framebuffers[image].Dispose();
            Framebuffers.Remove(image);
        }

        public override void OnDraw(Semaphore start, AttachmentImage image)
        {
            // Submit the command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.ColorAttachmentOutput, CommandBuffers[image], FinishedSemaphore
                );
        }
    }
}
