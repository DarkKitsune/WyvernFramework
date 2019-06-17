using VulkanCore;
using System.Numerics;

namespace WyvernFramework.Sprites
{
    public class SpriteInstance : RenderInstance
    {
        public Vector3 Position { get; }

        public Vector2 Scale { get; }

        public Texture2D Texture { get; }

        public Rect2D Rectangle { get; }

        public SpriteInstance(SpriteEffect effect, Vector3 position, Vector2 scale, Texture2D texture, Rect2D rectangle) : base(effect)
        {
            Position = position;
            Scale = scale;
            Texture = texture;
            Rectangle = rectangle;
            Register();
        }

        public override object GetListChoosingInformation()
        {
            return (Texture, Rectangle);
        }
    }
}
