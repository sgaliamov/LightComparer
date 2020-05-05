﻿using System;
using System.Reflection;
using ILLightenComparer.Emitters.Visitors;
using Illuminator;

namespace ILLightenComparer.Emitters.Variables
{
    internal sealed class FieldMemberVariable : IVariable
    {
        private FieldMemberVariable(FieldInfo fieldInfo) => FieldInfo = fieldInfo;

        public FieldInfo FieldInfo { get; }
        public Type OwnerType => FieldInfo.DeclaringType;
        public Type VariableType => FieldInfo.FieldType;

        public ILEmitter Load(VariableLoader visitor, ILEmitter il, ushort arg) => visitor.Load(this, il, arg);

        public ILEmitter LoadAddress(VariableLoader visitor, ILEmitter il, ushort arg) => visitor.LoadAddress(this, il, arg);

        public static IVariable Create(MemberInfo memberInfo) =>
            memberInfo is FieldInfo info
                ? new FieldMemberVariable(info)
                : null;
    }
}