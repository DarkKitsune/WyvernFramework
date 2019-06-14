using VulkanCore;

namespace WyvernFramework.Commands
{
    public class DrawCommand : Command
    {
        public int VertexCount { get; }
        public int InstanceCount { get; }
        public int FirstVertex { get; }
        public int FirstInstance { get; }

        public DrawCommand(int vertexCount, int instanceCount = 1, int firstVertex = 0, int firstInstance = 0, Command previous = default)
            : base(
                    PipelineStages.ColorAttachmentOutput,
                    null,
                    previous
                )
        {
            VertexCount = vertexCount;
            InstanceCount = instanceCount;
            FirstVertex = firstVertex;
            FirstInstance = firstInstance;
        }

        public override void RecordTo(CommandBuffer buffer)
        {
            buffer.CmdDraw(VertexCount, InstanceCount, FirstVertex, FirstInstance);
        }
    }
}
