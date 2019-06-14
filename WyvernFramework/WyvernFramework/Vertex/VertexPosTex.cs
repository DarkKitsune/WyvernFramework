using System.Numerics;
using VulkanCore;
using Spectrum;

namespace WyvernFramework.Vertex
{
    /// <summary>
    /// A vertex with a position component
    /// </summary>
    public struct VertexPosTex
    {
        public Vector3 Position { get; }
        public Vector2 TexCoord { get; }

        public VertexPosTex(Vector3 position, Vector2 texCoord)
        {
            Position = position;
            TexCoord = texCoord;
        }
    }
}
