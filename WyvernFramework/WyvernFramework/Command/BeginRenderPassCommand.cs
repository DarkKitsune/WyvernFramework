using VulkanCore;

namespace WyvernFramework.Commands
{
    public class BeginRenderPassCommand : Command
    {
        public RenderPassBeginInfo BeginInfo { get; }
        public SubpassContents SubpassContents { get; }

        public BeginRenderPassCommand(
                RenderPassBeginInfo beginInfo, SubpassContents subpassContents = SubpassContents.Inline,
                Command previous = default
            )
            : base(
                    PipelineStages.ColorAttachmentOutput,
                    null,
                    previous
                )
        {
            BeginInfo = beginInfo;
            SubpassContents = subpassContents;
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdBeginRenderPass(BeginInfo, SubpassContents);
        }
    }
}
