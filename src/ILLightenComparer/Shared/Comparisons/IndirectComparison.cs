﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Extensions;
using ILLightenComparer.Variables;
using Illuminator;
using static Illuminator.Functions;
using ILEmitterExtensions = ILLightenComparer.Extensions.ILEmitterExtensions;

namespace ILLightenComparer.Shared.Comparisons
{
    /// <summary>
    ///     Delegates comparison to static method or delayed compare method in context.
    /// </summary>
    internal sealed class IndirectComparison : IComparisonEmitter
    {
        private readonly MethodInfo _method;
        private readonly IVariable _variable;
        private readonly EmitterDelegate _checkForIntermediateResultEmitter;

        private IndirectComparison(EmitterDelegate checkForIntermediateResultEmitter, MethodInfo method, IVariable variable)
        {
            _checkForIntermediateResultEmitter = checkForIntermediateResultEmitter;
            _variable = variable;
            _method = method;
        }

        public static IndirectComparison Create(
            EmitterDelegate checkForIntermediateResultEmitter,
            Func<Type, MethodInfo> staticMethodFactory,
            MethodInfo genericDelayedMethod,
            IVariable variable)
        {
            var variableType = variable.VariableType;
            if (variableType != typeof(object) && variable is ArgumentVariable) {
                return null;
            }

            var typeOfVariableCanBeChangedOnRuntime = !variableType.IsSealedType();
            var compareMethod = typeOfVariableCanBeChangedOnRuntime
                ? genericDelayedMethod.MakeGenericMethod(variableType)
                : staticMethodFactory(variableType);

            return new IndirectComparison(checkForIntermediateResultEmitter, compareMethod, variable);
        }

        public static IndirectComparison Create(
            EmitterDelegate checkForIntermediateResultEmitter,
            MethodInfo genericDelayedMethod,
            IVariable variable)
        {
            var variableType = variable.VariableType;
            var compareMethod = genericDelayedMethod.MakeGenericMethod(variableType);

            return new IndirectComparison(checkForIntermediateResultEmitter, compareMethod, variable);
        }

        public ILEmitter Emit(ILEmitter il, Label _) =>
            il.CallMethod(
                Ldarg(Arg.Context),
                _method,
                new[] { _variable.VariableType, _variable.VariableType, typeof(CycleDetectionSet), typeof(CycleDetectionSet) },
                _variable.Load(Arg.X),
                _variable.Load(Arg.Y),
                Ldarg(Arg.SetX),
                Ldarg(Arg.SetY));

        public ILEmitter EmitCheckForResult(ILEmitter il, Label next) => _checkForIntermediateResultEmitter(il, next);
    }
}
