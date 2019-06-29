using System.Collections.Generic;
using System.Linq;

namespace WyvernFramework
{
    public class InstanceList
    {
        private List<RenderInstance> Instances { get; } = new List<RenderInstance>();

        public int Count => Instances.Count;

        public IEnumerable<RenderInstance> AllInstances => Instances;

        public bool Updated { get; private set; }

        public double LastUpdateTime { get; private set; }

        public InstanceRendererEffect Effect { get; }

        public double TimeSinceLastUpdate => Effect.Graphics.CurrentTime - LastUpdateTime;

        public InstanceList(InstanceRendererEffect effect)
        {
            Effect = effect;
        }

        public void FlagUpdate()
        {
            Updated = true;
        }

        internal void Update()
        {
            foreach (var inst in AllInstances.ToArray())
            {
                inst.StoreValues(0f);
            }
        }

        internal void FinishUpdate()
        {
            Updated = false;
            LastUpdateTime = Effect.Graphics.CurrentTime;
        }

        public void Add(RenderInstance instance)
        {
            Instances.Add(instance);
        }

        public void Remove(RenderInstance instance)
        {
            Instances.Remove(instance);
        }
    }
}
