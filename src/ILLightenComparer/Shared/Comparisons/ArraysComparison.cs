﻿using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Config;
using ILLightenComparer.Extensions;
using ILLightenComparer.Variables;
using Illuminator;

namespace ILLightenComparer.Shared.Comparisons
{
    internal sealed class ArraysComparison : IComparisonEmitter
    {
        private readonly int _defaultResult;
        private readonly ArrayComparisonEmitter _arrayComparisonEmitter;
        private readonly IConfigurationProvider _configuration;
        private readonly IVariable _variable;

        private ArraysComparison(int defaultResult, ArrayComparisonEmitter arrayComparisonEmitter, IConfigurationProvider configuration, IVariable variable)
        {
            _configuration = configuration;
            _variable = variable;
            _defaultResult = defaultResult;
            _arrayComparisonEmitter = arrayComparisonEmitter;
        }

        public static ArraysComparison Create(int defaultResult, ArrayComparisonEmitter arrayComparisonEmitter, IConfigurationProvider configuration, IVariable variable)
        {
            var variableType = variable.VariableType;
            if (variableType.IsArray && variableType.GetArrayRank() == 1) {
                return new ArraysComparison(defaultResult, arrayComparisonEmitter, configuration, variable);
            }

            return null;
        }

        public ILEmitter Emit(ILEmitter il, Label gotoNext)
        {
            var arrayType = _variable.VariableType;
            var (arrayX, arrayY) = _arrayComparisonEmitter.EmitLoad(_variable, il, gotoNext);

            il.EmitArrayLength(arrayType, arrayX, out var countX)
              .EmitArrayLength(arrayType, arrayY, out var countY);

            if (_configuration.Get(_variable.OwnerType).IgnoreCollectionOrder) {
                var elementType = arrayType.GetElementType();
                var hasCustomComparer = _configuration.HasCustomComparer(elementType);
                il.EmitArraySorting(hasCustomComparer, elementType, arrayX, arrayY);
            }

            return _arrayComparisonEmitter.EmitCompareArrays(il, arrayType, _variable.OwnerType, arrayX, arrayY, countX, countY, gotoNext);
        }

        public ILEmitter Emit(ILEmitter il) => il
            .DefineLabel(out var exit)
            .Execute(this.Emit(exit))
            .MarkLabel(exit)
            .Return(_defaultResult);

        public ILEmitter EmitCheckForIntermediateResult(ILEmitter il, Label _) => il;
    }
}
