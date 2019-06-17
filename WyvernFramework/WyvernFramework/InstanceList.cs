using System.Collections.Generic;

namespace WyvernFramework
{
    public class InstanceList
    {
        public const int MaxInstances = 4000;

        private List<RenderInstance> Instances { get; } = new List<RenderInstance>();

        public int Count => Instances.Count;

        public IEnumerable<RenderInstance> AllInstances => Instances;

        public bool Updated { get; private set; }

        public void FlagUpdate()
        {
            Updated = true;
        }

        public void ClearUpdate()
        {
            Updated = false;
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
