using VulkanCore;
using System.Numerics;

namespace WyvernFramework.Sprites
{
    public class SpriteInstance : RenderInstance
    {
        internal Vector3 StoredPosition;

        private Vector3 PositionDelta => Velocity * (float)TimeSinceLastStore;

        public Vector3 Position
        {
            get => StoredPosition + PositionDelta;
            set
            {
                StoreValues();
                StoredPosition = value;
            }
        }

        public Vector3 Velocity { get; }

        public Vector2 Scale { get; }

        public Texture2D Texture { get; }

        public Vector4 Rectangle { get; }

        public Animation Animation { get; }

        public float AnimationTime { get; }

        public SpriteInstance(SpriteEffect effect, Vector3 position, Vector3 velocity, Vector2 scale, Texture2D texture, Rect2D rectangle, Animation animation) : base(effect)
        {
            StoredPosition = position;
            Velocity = velocity;
            Scale = scale;
            Texture = texture;
            var texExtent = texture.Image.Extent;
            Rectangle = new Vector4(
                    rectangle.Offset.X / (float)texExtent.Width,
                    rectangle.Offset.Y / (float)texExtent.Height,
                    rectangle.Extent.Width / (float)texExtent.Width,
                    rectangle.Extent.Height / (float)texExtent.Height
                );
            Animation = animation;
            AnimationTime = 0f;
            Register();
        }

        public override object GetListChoosingInformation()
        {
            return (Texture, Animation);
        }

        protected override void OnStoreValues()
        {
            StoredPosition = Position;
        }
    }
}
