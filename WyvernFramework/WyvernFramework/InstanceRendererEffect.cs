﻿using System.Collections.Generic;
using System.Linq;
using VulkanCore;

namespace WyvernFramework
{
    public class InstanceRendererEffect : ImageEffect
    {
        protected Dictionary<object, InstanceList> InstanceLists { get; } = new Dictionary<object, InstanceList>();

        protected bool AnyUpdatedInstanceLists => UpdatedInstanceLists.Any();

        protected IEnumerable<KeyValuePair<object, InstanceList>> UpdatedInstanceLists => InstanceLists.Where(e => e.Value.Updated);

        public InstanceRendererEffect(
                string name, Graphics graphics,
                ImageLayout finalLayout, Accesses finalAccess, PipelineStages finalStage, ImageLayout initialLayout = ImageLayout.Undefined,
                Accesses initialAccess = Accesses.None, PipelineStages initialStage = PipelineStages.TopOfPipe
            )
                : base(name, graphics, finalLayout, finalAccess, finalStage, initialLayout, initialAccess, initialStage)
        {

        }

        public InstanceList RegisterInstance(RenderInstance instance)
        {
            var listChoosingInfo = instance.GetListChoosingInformation();
            var list = GetInstanceList(listChoosingInfo);
            list.Add(instance);
            list.FlagUpdate();
            return list;
        }

        private InstanceList GetInstanceList(object listChoosingInfo)
        {
            if (InstanceLists.TryGetValue(listChoosingInfo, out var list))
                return list;
            var newList = new InstanceList(this);
            InstanceLists.Add(listChoosingInfo, newList);
            return newList;
        }

        protected void UpdateLists()
        {
            foreach (var keyList in UpdatedInstanceLists)
            {
                if (keyList.Value.Updated)
                    keyList.Value.Update();
            }
        }

        protected void FinishUpdateLists()
        {
            foreach (var keyList in UpdatedInstanceLists)
            {
                keyList.Value.FinishUpdate();
            }
        }
    }
}
