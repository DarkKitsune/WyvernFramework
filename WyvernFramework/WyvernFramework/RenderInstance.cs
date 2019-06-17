using System;

namespace WyvernFramework
{
    public class RenderInstance
    {
        public InstanceRendererEffect InstanceRendererEffect { get; }
        public InstanceList InstanceList { get; private set; }
        public bool Registered { get; private set; }

        public RenderInstance(InstanceRendererEffect effect)
        {
            InstanceRendererEffect = effect;
        }

        public void Register()
        {
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
    }
}
