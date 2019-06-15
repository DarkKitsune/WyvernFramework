using VulkanCore;

namespace WyvernFramework.Commands
{
    /// <summary>
    /// Transition an image to a new layout
    /// </summary>
    public class TransitionImageCommand : Command
    {
        public TransitionImageCommand(Image image, ImageSubresourceRange range, ImageLayout layout, Accesses access, Command previous = default)
            : base(
                    PipelineStages.BottomOfPipe,
                    new CommandImageLayout(image, range, layout, access),
                    previous
                )
        {
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdPipelineBarrier(
                    GenerateImageMemoryBarrierSrcStageMask(),
                    Stage,
                    imageMemoryBarriers: new[] { GenerateImageMemoryBarrier() }
                );
        }
    }
}
