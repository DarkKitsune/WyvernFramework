using System;

namespace WyvernFramework
{
    public class RenderInstance
    {
        public InstanceRendererEffect InstanceRendererEffect { get; }
        public InstanceList InstanceList { get; private set; }
        public bool Registered { get; private set; }

        internal double LastStoreTime { get; private set; }

        internal double TimeSinceLastStore => InstanceRendererEffect.Graphics.CurrentTime - LastStoreTime;

        public RenderInstance(InstanceRendererEffect effect)
        {
            InstanceRendererEffect = effect;
            LastStoreTime = InstanceRendererEffect.Graphics.CurrentTime;
        }

        public void Register()
        {
            LastStoreTime = InstanceRendererEffect.Graphics.CurrentTime;
            InstanceList = InstanceRendererEffect.RegisterInstance(this);
            Registered = true;
        }

        public virtual object GetListChoosingInformation()
        {
            return null;
        }

        public void FlagUpdate()
        {
            if (!Registered)
                throw new InvalidOperationException("Instance is not registered or has been deleted");
            InstanceList.FlagUpdate();
        }

        public void Delete()
        {
            Registered = false;
            InstanceList.Remove(this);
        }

        /// <summary>
        /// Store current values of changing values in instance
        /// </summary>
        public void StoreValues(float time = -1f)
        {
            OnStoreValues();
            LastStoreTime = time < 0f ? InstanceList.TimeSinceLastUpdate : time;
            FlagUpdate();
        }

        /// <summary>
        /// Called when storing current values of changing values in instance
        /// </summary>
        protected virtual void OnStoreValues()
        {
        }
    }
}
