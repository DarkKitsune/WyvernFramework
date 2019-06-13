using VulkanCore;

namespace WyvernFramework.Commands
{
    public class ClearColorCommand : Command
    {
        /// <summary>
        /// Image to clear
        /// </summary>
        public Image Image { get; }

        /// <summary>
        /// Image subresource range to clear
        /// </summary>
        public ImageSubresourceRange Range { get; }

        /// <summary>
        /// Value to clear image with
        /// </summary>
        public ClearColorValue Color { get; }

        public ClearColorCommand(Image image, ImageSubresourceRange range, ClearColorValue color, Command previous = default)
            : base(
                    PipelineStages.Transfer,
                    new CommandImageLayout(image, range, ImageLayout.TransferDstOptimal, Accesses.MemoryWrite),
                    previous
                )
        {
            Image = image;
            Range = range;
            Color = color;
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdPipelineBarrier(
                    GenerateImageMemoryBarrierSrcStageMask(),
                    Stage,
                    imageMemoryBarriers: new[] { GenerateImageMemoryBarrier() }
                );
            buffer.CmdClearColorImage(Image, ImageLayout.TransferDstOptimal, Color, Range);
        }
    }
}
