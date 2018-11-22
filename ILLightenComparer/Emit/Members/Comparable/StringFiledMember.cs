﻿using System.Reflection;
using ILLightenComparer.Emit.Emitters;

namespace ILLightenComparer.Emit.Members.Comparable
{
    internal sealed class StringFiledMember : ComparableFieldMember
    {
        public StringFiledMember(FieldInfo fieldInfo, MethodInfo compareToMethod) 
            : base(fieldInfo, compareToMethod) { }

        public override void Accept(StackEmitter visitor, ILEmitter il)
        {
            visitor.Visit(this, il);
        }
    }
}
