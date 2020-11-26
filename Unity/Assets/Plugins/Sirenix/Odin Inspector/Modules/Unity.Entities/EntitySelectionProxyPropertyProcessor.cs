//-----------------------------------------------------------------------
// <copyright file="EntitySelectionProxyPropertyProcessor.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System.Collections.Generic;
    using Unity.Entities.Editor;
    using Sirenix.OdinInspector.Editor;

    public class EntitySelectionProxyPropertyProcessor : OdinPropertyProcessor<EntitySelectionProxy>
    {
        public override void ProcessMemberProperties(List<InspectorPropertyInfo> propertyInfos)
        {
            EntityInspectionData data = new EntityInspectionData()
            {
                EntityManager = (this.Property.ValueEntry.WeakSmartValue as EntitySelectionProxy).EntityManager
            };

            propertyInfos.AddValue(
                name: "Entity",
                getter: () =>
                {
                    data.Entity = (this.Property.ValueEntry.WeakSmartValue as EntitySelectionProxy).Entity;
                    return data;
                },
                setter: (value) => data = value);
        }
    }
}
