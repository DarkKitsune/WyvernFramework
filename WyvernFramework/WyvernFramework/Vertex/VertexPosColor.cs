using System.Numerics;
using VulkanCore;
using Spectrum;

namespace WyvernFramework.Vertex
{
    /// <summary>
    /// A vertex with a position component
    /// </summary>
    public struct VertexPosColor
    {
        public Vector3 Position { get; }
        public ColorF4 Color { get; }

        public VertexPosColor(Vector3 position, ColorF4 color)
        {
            Position = position;
            Color = color;
        }

        public VertexPosColor(Vector3 position, Color.RGB color) : this(position, color.ToVulkanCore())
        {
        }

        public VertexPosColor(Vector3 position, Color.HSV color) : this(position, color.ToVulkanCore())
        {
        }

        public VertexPosColor(Vector3 position, Color.HSL color) : this(position, color.ToVulkanCore())
        {
        }
    }
}
