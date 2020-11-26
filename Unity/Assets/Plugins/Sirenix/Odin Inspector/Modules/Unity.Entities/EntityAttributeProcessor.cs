//-----------------------------------------------------------------------
// <copyright file="EntityAttributeProcessor.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System;
    using System.Collections.Generic;
    using Unity.Entities;
    using Sirenix.OdinInspector.Editor;
    using System.Reflection;

    public class EntityAttributeProcessor : OdinAttributeProcessor<Entity>
    {
        public override bool CanProcessChildMemberAttributes(InspectorProperty parentProperty, MemberInfo member)
        {
            return false;
        }

        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.GetOrAddAttribute<InlinePropertyAttribute>();
        }
    }
}
