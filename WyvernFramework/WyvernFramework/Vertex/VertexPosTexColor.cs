using System.Numerics;
using VulkanCore;
using Spectrum;

namespace WyvernFramework.Vertex
{
    /// <summary>
    /// A vertex with a position component
    /// </summary>
    public struct VertexPosTexColor
    {
        public Vector3 Position { get; }
        public Vector2 TexCoord { get; }
        public ColorF4 Color { get; }

        public VertexPosTexColor(Vector3 position, Vector2 texCoord, ColorF4 color)
        {
            Position = position;
            TexCoord = texCoord;
            Color = color;
        }

        public VertexPosTexColor(Vector3 position, Vector2 texCoord, Color.RGB color) : this(position, texCoord, color.ToVulkanCore())
        {
        }

        public VertexPosTexColor(Vector3 position, Vector2 texCoord, Color.HSV color) : this(position, texCoord, color.ToVulkanCore())
        {
        }

        public VertexPosTexColor(Vector3 position, Vector2 texCoord, Color.HSL color) : this(position, texCoord, color.ToVulkanCore())
        {
        }
    }
}
