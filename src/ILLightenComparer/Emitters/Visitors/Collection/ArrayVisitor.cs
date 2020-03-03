﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using ILLightenComparer.Config;
using ILLightenComparer.Emitters.Comparisons;
using Illuminator;
using Illuminator.Extensions;

namespace ILLightenComparer.Emitters.Visitors.Collection
{
    internal sealed class ArrayVisitor
    {
        private readonly ArrayComparer _arrayComparer;
        private readonly CollectionComparer _collectionComparer;
        private readonly IConfigurationProvider _configurations;

        public ArrayVisitor(
            IConfigurationProvider configurations,
            CompareVisitor compareVisitor,
            VariableLoader loader,
            ComparisonsProvider comparisons)
        {
            _configurations = configurations;
            _collectionComparer = new CollectionComparer(configurations, loader);
            _arrayComparer = new ArrayComparer(compareVisitor, comparisons);
        }

        public ILEmitter Visit(ArraysComparison comparison, ILEmitter il, Label afterLoop)
        {
            var variable = comparison.Variable;
            var variableType = variable.VariableType;

            var (x, y) = _collectionComparer.EmitLoad(comparison, il, afterLoop);
            var (countX, countY) = _arrayComparer.EmitLoadCounts(variableType, x, y, il);

#if DEBUG
            EmitCheckForNegativeCount(countX, countY, comparison.Variable.VariableType, il);
#endif

            if (_configurations.Get(variable.OwnerType).IgnoreCollectionOrder) {
                _collectionComparer.EmitArraySorting(il, variableType.GetElementType(), x, y);
            }

            return _arrayComparer.Compare(variableType, variable.OwnerType, x, y, countX, countY, il, afterLoop);
        }

        private static void EmitCheckForNegativeCount(
            LocalVariableInfo countX,
            LocalVariableInfo countY,
            MemberInfo memberType,
            ILEmitter il)
        {
            il.Block((il, loopInit) => il
                .Block((il, negativeException) => il
                    .Greater(
                        il => il.LoadInteger(0),
                        il => il.LoadLocal(countX),
                        negativeException)
                    .LessOrEqual(
                        il => il.LoadInteger(0),
                        il => il.LoadLocal(countY),
                        loopInit))
                .LoadString($"Collection {memberType.DisplayName()} has negative count of elements.")
                .New(typeof(IndexOutOfRangeException).GetConstructor(new[] { typeof(string) }))
                .Throw()
            );
        }
    }
}
