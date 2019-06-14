using VulkanCore;

namespace WyvernFramework.Commands
{
    public class BindPipelineCommand : Command
    {
        public PipelineBindPoint BindPoint { get; }
        public Pipeline Pipeline { get; }

        public BindPipelineCommand(PipelineBindPoint bindPoint, Pipeline pipeline, Command previous = default)
            : base(
                    PipelineStages.ColorAttachmentOutput,
                    null,
                    previous
                )
        {
            BindPoint = bindPoint;
            Pipeline = pipeline;
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdBindPipeline(BindPoint, Pipeline);
        }
    }
}
