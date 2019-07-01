using VulkanCore;
using System;
using System.Numerics;

namespace WyvernFramework.Sprites
{
    public class SpriteInstance : RenderInstance
    {
        internal Vector3 StoredPosition;
        internal Vector3 StoredVelocity;
        internal Vector2 StoredScale;
        internal float StoredRotation;

        public Vector3 Position
        {
            get => StoredPosition + Velocity * (float)TimeSinceLastStore;
            set
            {
                StoreValues();
                StoredPosition = value;
            }
        }

        public Vector3 Velocity
        {
            get => StoredVelocity;
            set
            {
                StoreValues();
                StoredVelocity = value;
            }
        }

        public Vector2 Scale
        {
            get => (Animation is null) ? StoredScale : Animation.GetScale(AnimationTime, StoredScale);
            set
            {
                StoreValues();
                StoredScale = value;
            }
        }

        public float Rotation
        {
            get => (Animation is null) ? StoredRotation : Animation.GetRotation(AnimationTime, StoredRotation);
            set
            {
                StoreValues();
                StoredRotation = value;
            }
        }

        public Texture2D Texture { get; }

        public Vector4 Rectangle { get; }

        public Animation Animation { get; }

        public double AnimationStartTime { get; }

        public float AnimationTime => (float)(InstanceRendererEffect.Graphics.CurrentTime - AnimationStartTime);

        public SpriteInstance(SpriteEffect effect, Vector3 position, Vector3 velocity, Vector2 scale, Texture2D texture, Rect2D rectangle, Animation animation) : base(effect)
        {
            StoredPosition = position;
            StoredVelocity = velocity;
            StoredScale = scale;
            Texture = texture;
            var texExtent = texture.Image.Extent;
            Rectangle = new Vector4(
                    rectangle.Offset.X / (float)texExtent.Width,
                    rectangle.Offset.Y / (float)texExtent.Height,
                    rectangle.Extent.Width / (float)texExtent.Width,
                    rectangle.Extent.Height / (float)texExtent.Height
                );
            Animation = animation;
            AnimationStartTime = InstanceRendererEffect.Graphics.CurrentTime;
            Register();
        }

        public override object GetListChoosingInformation()
        {
            return (Texture, Animation);
        }

        protected override void OnStoreValues()
        {
            if (InstanceList == null)
                throw new InvalidOperationException("Trying to store values while not registered yet; check that constructor " +
                    "isn't setting any property that would call StoreValues()");
            StoredPosition = Position;
            StoredVelocity = Velocity;
            StoredScale = Scale;
            StoredRotation = Rotation;
        }


    }
}
