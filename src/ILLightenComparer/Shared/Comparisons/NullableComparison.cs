﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Variables;
using Illuminator;
using Illuminator.Extensions;

namespace ILLightenComparer.Shared.Comparisons
{
    internal sealed class NullableComparison : IComparisonEmitter
    {
        private readonly IResolver _resolver;
        private readonly int _defaultResult;
        private readonly EmitterDelegate _checkForIntermediateResultEmitter;
        private readonly EmitCheckNullablesForValueDelegate _emitCheckNullablesForValue;
        private readonly IVariable _variable;

        private NullableComparison(
            IResolver resolver,
            int defaultResult,
            EmitterDelegate checkForIntermediateResultEmitter,
            EmitCheckNullablesForValueDelegate emitCheckNullablesForValue,
            IVariable variable)
        {
            _resolver = resolver;
            _defaultResult = defaultResult;
            _checkForIntermediateResultEmitter = checkForIntermediateResultEmitter;
            _emitCheckNullablesForValue = emitCheckNullablesForValue;
            _variable = variable ?? throw new ArgumentNullException(nameof(variable));
        }

        public static NullableComparison Create(
            IResolver resolver,
            int defaultResult,
            EmitterDelegate checkForIntermediateResultEmitter,
            EmitCheckNullablesForValueDelegate emitCheckNullablesForValue,
            IVariable variable)
        {
            if (variable.VariableType.IsNullable()) {
                return new NullableComparison(resolver, defaultResult, checkForIntermediateResultEmitter, emitCheckNullablesForValue, variable);
            }

            return null;
        }

        public ILEmitter Emit(ILEmitter il, Label gotoNext)
        {
            var variableType = _variable.VariableType;

            _variable.Load(il, Arg.X).Store(variableType, out var nullableX);
            _variable.Load(il, Arg.Y).Store(variableType, out var nullableY);
            _emitCheckNullablesForValue(il, nullableX, nullableY, variableType, gotoNext);

            var nullableVariables = new NullableVariables(variableType, _variable.OwnerType, new Dictionary<ushort, LocalBuilder>(2) {
                [Arg.X] = nullableX,
                [Arg.Y] = nullableY
            });

            return _resolver
                .GetComparisonEmitter(nullableVariables)
                .Emit(il, gotoNext);
        }

        public ILEmitter Emit(ILEmitter il) => il
            .DefineLabel(out var exit)
            .Execute(this.Emit(exit))
            .Execute(this.EmitCheckForIntermediateResult(exit))
            .MarkLabel(exit)
            .Return(_defaultResult);

        public ILEmitter EmitCheckForIntermediateResult(ILEmitter il, Label next) => _checkForIntermediateResultEmitter(il, next);
    }
}