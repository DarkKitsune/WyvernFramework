using VulkanCore;

namespace WyvernFramework
{
    public static class BlendStates
    {
        public static PipelineColorBlendAttachmentState Replace { get; } = new PipelineColorBlendAttachmentState(
                blendEnable: false
            );
        public static PipelineColorBlendAttachmentState Add { get; } = new PipelineColorBlendAttachmentState(
                 blendEnable: true,
                 srcColorBlendFactor: BlendFactor.One,
                 dstColorBlendFactor: BlendFactor.One,
                 colorBlendOp: BlendOp.Add,
                 srcAlphaBlendFactor: BlendFactor.One,
                 dstAlphaBlendFactor: BlendFactor.One,
                 alphaBlendOp: BlendOp.Add,
                 colorWriteMask: ColorComponents.All
             );
        public static PipelineColorBlendAttachmentState Alpha { get; } = new PipelineColorBlendAttachmentState(
                 blendEnable: true,
                 srcColorBlendFactor: BlendFactor.SrcAlpha,
                 dstColorBlendFactor: BlendFactor.OneMinusSrcAlpha,
                 colorBlendOp: BlendOp.Add,
                 srcAlphaBlendFactor: BlendFactor.One,
                 dstAlphaBlendFactor: BlendFactor.OneMinusSrcAlpha,
                 alphaBlendOp: BlendOp.Add,
                 colorWriteMask: ColorComponents.All
             );
        public static PipelineColorBlendAttachmentState AlphaPremultiplied { get; } = new PipelineColorBlendAttachmentState(
                 blendEnable: true,
                 srcColorBlendFactor: BlendFactor.One,
                 dstColorBlendFactor: BlendFactor.OneMinusSrcAlpha,
                 colorBlendOp: BlendOp.Add,
                 srcAlphaBlendFactor: BlendFactor.One,
                 dstAlphaBlendFactor: BlendFactor.OneMinusSrcAlpha,
                 alphaBlendOp: BlendOp.Add,
                 colorWriteMask: ColorComponents.All
             );
    }
}
