using System.Numerics;

namespace WyvernFramework.Vertex
{
    /// <summary>
    /// A vertex with a position component
    /// </summary>
    public struct VertexPos
    {
        public Vector3 Position { get; }

        public VertexPos(Vector3 position)
        {
            Position = position;
        }
    }
}
