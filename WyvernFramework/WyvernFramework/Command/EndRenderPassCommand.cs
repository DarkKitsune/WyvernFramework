using VulkanCore;

namespace WyvernFramework.Commands
{
    public class EndRenderPassCommand : Command
    {
        public EndRenderPassCommand(Command previous = default)
            : base(
                    PipelineStages.ColorAttachmentOutput,
                    null,
                    previous
                )
        {
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdEndRenderPass();
        }
    }
}
