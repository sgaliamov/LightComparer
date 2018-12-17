﻿using System;
using System.Reflection;
using ILLightenComparer.Emit.Emitters.Variables;
using ILLightenComparer.Emit.Emitters.Visitors;
using ILLightenComparer.Emit.Extensions;
using ILLightenComparer.Emit.Reflection;

namespace ILLightenComparer.Emit.Emitters.Comparisons
{
    internal sealed class CollectionComparison : ICompareVisitorAcceptor, ICompareEmitterAcceptor
    {
        private CollectionComparison(IVariable variable)
        {
            Variable = variable;

            var variableType = variable.VariableType;
            GetLengthMethod = variableType.GetPropertyGetter(MethodName.ArrayLength);
            GetItemMethod = variableType.GetMethod(MethodName.ArrayGet, new[] { typeof(int) });
            ElementType = variableType.GetElementType();
        }

        public Type ElementType { get; }
        public MethodInfo GetItemMethod { get; }
        public MethodInfo GetLengthMethod { get; }

        public ILEmitter Accept(CompareEmitter visitor, ILEmitter il)
        {
            return visitor.Visit(this, il);
        }

        public IVariable Variable { get; }

        public ILEmitter Accept(CompareVisitor visitor, ILEmitter il)
        {
            return visitor.Visit(this, il);
        }

        public static CollectionComparison Create(MemberInfo memberInfo)
        {
            var variable = VariableFactory.Create(memberInfo);

            var underlyingType = variable.VariableType.GetUnderlyingType();
            if (underlyingType.IsArray && underlyingType.GetArrayRank() == 1)
            {
                return new CollectionComparison(variable);
            }

            return null;
        }
    }
}
