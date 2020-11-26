//-----------------------------------------------------------------------
// <copyright file="AABBAttributeProcessor.cs" company="Sirenix IVS">
// Copyright (c) Sirenix IVS. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Sirenix.OdinInspector.Modules.Entities
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector.Editor;
    using System.Reflection;
    using Unity.Mathematics;

    public class AABBAttributeProcessor : OdinAttributeProcessor<AABB>
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
