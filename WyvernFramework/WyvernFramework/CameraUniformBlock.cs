using System.Numerics;
using System.Runtime.InteropServices;

namespace WyvernFramework
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct CameraUniformBlock
    {
        [FieldOffset(0)]
        public Matrix4x4 View;

        [FieldOffset(64)]
        public Matrix4x4 Projection;
    }
}
