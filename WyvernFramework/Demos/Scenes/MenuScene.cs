using System;
using WyvernFramework;
using WyvernFramework.Commands;
using VulkanCore;
using Spectrum;
using System.IO;
using System.Linq;

namespace Demos.Scenes
{
    /// <summary>
    /// The demo menu scene
    /// </summary>
    public class MenuScene : Scene
    {
        public override string Description => "The demo menu scene";

        /// <summary>
        /// Command buffers to clear the swapchain images
        /// </summary>
        private CommandBuffer[] ClearCommandBuffers;

        /// <summary>
        /// Semaphore to be signaled when we're done clearing the image
        /// </summary>
        private Semaphore ClearedSemaphore;
        
        /// <summary>
        /// The triangle vertex shader
        /// </summary>
        private ShaderModule TriangleVertexShader;

        /// <summary>
        /// The triangle fragment shader
        /// </summary>
        private ShaderModule TriangleFragmentShader;

        /// <summary>
        /// The triangle graphics pipeline
        /// </summary>
        private Pipeline TrianglePipeline;

        /// <summary>
        /// The triangle render pass
        /// </summary>
        private RenderPass TriangleRenderPass;

        /// <summary>
        /// The triangle image views
        /// </summary>
        private ImageView[] TriangleImageViews;

        /// <summary>
        /// The triangle framebuffers
        /// </summary>
        private Framebuffer[] TriangleFramebuffers;

        /// <summary>
        /// Command buffers to draw the triangle
        /// </summary>
        private CommandBuffer[] TriangleCommandBuffers;

        /// <summary>
        /// The semaphore to be signaled when we're done drawing the triangle
        /// </summary>
        private Semaphore TriangleSemaphore;

        public MenuScene(WyvernWindow window) : base("Menu", window)
        {
        }

        /// <summary>
        /// Called when starting the scene
        /// </summary>
        public override void OnStart()
        {
            // Create triangle pipeline
            {
                // Load shaders
                {
                    var vertPath = Path.Combine("..", "..", "..", "Content", "Shader.vert.spv");
                    var fragPath = Path.Combine("..", "..", "..", "Content", "Shader.frag.spv");
                    TriangleVertexShader = Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(
                            File.ReadAllBytes(vertPath)
                        ));
                    TriangleFragmentShader = Graphics.Device.CreateShaderModule(new ShaderModuleCreateInfo(
                            File.ReadAllBytes(fragPath)
                        ));
                }
                // Create render pass
                {
                    TriangleRenderPass = Graphics.Device.CreateRenderPass(new RenderPassCreateInfo(
                        subpasses: new[]
                        {
                            new SubpassDescription(
                                    new AttachmentReference[] {
                                        new AttachmentReference(0, ImageLayout.ColorAttachmentOptimal)
                                    }
                                )
                        },
                        attachments: new[]
                        {
                            new AttachmentDescription(
                                    flags: 0,
                                    format: Graphics.SwapchainImageFormat,
                                    samples: SampleCounts.Count1,
                                    loadOp: AttachmentLoadOp.Load,
                                    storeOp: AttachmentStoreOp.Store,
                                    stencilLoadOp: AttachmentLoadOp.DontCare,
                                    stencilStoreOp: AttachmentStoreOp.DontCare,
                                    initialLayout: ImageLayout.TransferDstOptimal,
                                    finalLayout: ImageLayout.PresentSrcKhr
                                )
                        }
                    ));
                }
                // Create image views
                {
                    TriangleImageViews = Graphics.SwapchainImages.Select(
                            e => e.CreateView(new ImageViewCreateInfo(
                                    format: Graphics.SwapchainImageFormat,
                                    subresourceRange: new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1)
                                ))
                        ).ToArray();
                }
                // Create framebuffers
                {
                    TriangleFramebuffers = new Framebuffer[Graphics.SwapchainImages.Length];
                    for (var imageIndex = 0; imageIndex < Graphics.SwapchainImages.Length; imageIndex++)
                    {
                        TriangleFramebuffers[imageIndex] = TriangleRenderPass.CreateFramebuffer(new FramebufferCreateInfo(
                                attachments: new[] { TriangleImageViews[imageIndex] },
                                width: Graphics.SwapchainExtent.Width,
                                height: Graphics.SwapchainExtent.Height
                            ));
                    }
                }
                // Create graphics pipeline
                {
                    TrianglePipeline = Graphics.Device.CreateGraphicsPipeline(new GraphicsPipelineCreateInfo(
                            layout: Graphics.Device.CreatePipelineLayout(new PipelineLayoutCreateInfo()),
                            renderPass: TriangleRenderPass,
                            subpass: 0,
                            stages: new[]
                            {
                                new PipelineShaderStageCreateInfo(ShaderStages.Vertex, TriangleVertexShader, "main"),
                                new PipelineShaderStageCreateInfo(ShaderStages.Fragment, TriangleFragmentShader, "main")
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
                                    new Viewport(0f, 0f, Window.Size.X, Window.Size.Y),
                                    new Rect2D(0, 0, (int)Window.Size.X, (int)Window.Size.Y)
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
            // Create a clear command buffer per swapchain image
            ClearCommandBuffers = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, Graphics.SwapchainImages.Length);
            // Create a semaphore for when we're done clearing
            ClearedSemaphore = Graphics.Device.CreateSemaphore();
            // Create a triangle command buffer per swapchain image
            {
                // Create buffers
                TriangleCommandBuffers = Graphics.GraphicsQueueFamily.CreateCommandBuffers(CommandBufferLevel.Primary, Graphics.SwapchainImages.Length);
                // Record buffers
                for (var imageIndex = 0; imageIndex < TriangleCommandBuffers.Length; imageIndex++)
                {
                    // Acquire command buffer
                    var buffer = TriangleCommandBuffers[imageIndex];
                    // Begin recording
                    buffer.Begin(new CommandBufferBeginInfo());
                    // Write commands
                    var commands =
                            new BeginRenderPassCommand(new RenderPassBeginInfo(
                                    TriangleFramebuffers[imageIndex],
                                    TriangleRenderPass,
                                    new Rect2D(0, 0, (int)Window.Size.X, (int)Window.Size.Y)
                                ))
                        +   new BindPipelineCommand(PipelineBindPoint.Graphics, TrianglePipeline)
                        +   new DrawCommand(3)
                        +   new EndRenderPassCommand();
                    // Record commands to buffer
                    commands.RecordTo(buffer);
                    // Finish recording
                    buffer.End();
                }
            }
            // Create a semaphore for when we're done drawing the triangle
            TriangleSemaphore = Graphics.Device.CreateSemaphore();
        }

        /// <summary>
        /// Called when ending the scene
        /// </summary>
        public override void OnEnd()
        {
            // Dispose of clear buffers
            foreach (var buffer in ClearCommandBuffers)
                buffer.Dispose();
            // Dispose of clear semaphore
            ClearedSemaphore.Dispose();
        }

        /// <summary>
        /// Called when updating the scene
        /// </summary>
        public override void OnUpdate()
        {
            // Update clear color for each swapchain image
            for (var imageIndex = 0; imageIndex < Graphics.SwapchainImages.Length; imageIndex++)
                SetClearColor(imageIndex);
        }

        /// <summary>
        /// Called when drawing the scene
        /// </summary>
        /// <param name="imageIndex"></param>
        /// <param name="start"></param>
        /// <param name="finished"></param>
        public override void OnDraw(Semaphore start, int imageIndex, out Semaphore finished)
        {
            // Clear screen by clearing the swapchain image
            ClearScreen(start, imageIndex);
            DrawTriangle(ClearedSemaphore, imageIndex);
            // We are finished when ClearedSemaphore is signaled
            finished = TriangleSemaphore;
        }

        /// <summary>
        /// Set the clear color for a swapchain image
        /// </summary>
        /// <param name="imageIndex"></param>
        private void SetClearColor(int imageIndex)
        {
            // Create the clear command buffer for the image
            {
                // Generate clear color based on time
                var hue = (DateTime.Now.Ticks / (double)TimeSpan.TicksPerSecond) * 45.0;
                var color = new Color.HSV(hue % 360.0, 1.0, 1.0).ToRGB();
                var clearColor = new ClearColorValue(color.R / 255f, color.G / 255f, color.B / 255f);
                // Record clear command buffer
                {
                    // Acquire image and command buffer
                    var image = Graphics.SwapchainImages[imageIndex];
                    var buffer = ClearCommandBuffers[imageIndex];
                    // Specify subresource range we will be working with
                    var range = new ImageSubresourceRange(ImageAspects.Color, 0, 1, 0, 1);
                    // Begin recording
                    buffer.Begin();
                    // Generate commands
                    var commands =
                            new ClearColorCommand(image, range, clearColor);
                    commands.RecordTo(buffer);
                    // Finish recording
                    buffer.End();
                }
            }
        }

        /// <summary>
        /// Clear a swapchain image
        /// </summary>
        /// <param name="start"></param>
        /// <param name="imageIndex"></param>
        private void ClearScreen(Semaphore start, int imageIndex)
        {
            // Submit the clear command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.Transfer, ClearCommandBuffers[imageIndex], ClearedSemaphore
                );
        }

        /// <summary>
        /// Draw the triangle
        /// </summary>
        /// <param name="start"></param>
        /// <param name="imageIndex"></param>
        private void DrawTriangle(Semaphore start, int imageIndex)
        {
            // Submit the triangle command buffer
            Graphics.GraphicsQueueFamily.First.Submit(
                    start, PipelineStages.ColorAttachmentOutput, TriangleCommandBuffers[imageIndex], TriangleSemaphore
                );
        }
    }
}
