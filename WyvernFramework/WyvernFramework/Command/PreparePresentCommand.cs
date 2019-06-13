using VulkanCore;

namespace WyvernFramework.Commands
{
    /// <summary>
    /// Prepare an image for presenting
    /// </summary>
    public class PreparePresentCommand : Command
    {
        public PreparePresentCommand(Image image, ImageSubresourceRange range, Command previous = default)
            : base(
                    PipelineStages.BottomOfPipe,
                    new CommandImageLayout(image, range, ImageLayout.PresentSrcKhr, Accesses.MemoryRead),
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
