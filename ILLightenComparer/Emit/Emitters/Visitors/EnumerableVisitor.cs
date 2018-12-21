﻿using System;
using System.Reflection.Emit;
using ILLightenComparer.Emit.Emitters.Comparisons;
using ILLightenComparer.Emit.Emitters.Variables;
using ILLightenComparer.Emit.Extensions;
using ILLightenComparer.Emit.Reflection;

namespace ILLightenComparer.Emit.Emitters.Visitors
{
    internal sealed class EnumerableVisitor : CollectionVisitor
    {
        private const int LocalDoneX = 3;
        private const int LocalDoneY = 4;

        private readonly CompareVisitor _compareVisitor;
        private readonly ComparerContext _context;
        private readonly Converter _converter;
        private readonly StackVisitor _stackVisitor;

        public EnumerableVisitor(
            ComparerContext context,
            StackVisitor stackVisitor,
            CompareVisitor compareVisitor,
            VariableLoader loader,
            Converter converter)
            : base(stackVisitor, compareVisitor, loader)
        {
            _context = context;
            _compareVisitor = compareVisitor;
            _converter = converter;
            _stackVisitor = stackVisitor;
        }

        public ILEmitter Visit(EnumerableComparison comparison, ILEmitter il)
        {
            var variable = comparison.Variable;
            var (x, y, gotoNext) = EmitLoad(il, comparison);

            if (_context.GetConfiguration(variable.OwnerType).IgnoreCollectionOrder)
            {
                EmitCollectionsSorting(il, variable.VariableType, x, y);
            }

            var (xEnumerator, yEnumerator) = EmitLoadEnumerators(il, comparison, x, y);

            // todo: think how to use it, the problem now with the inner `return` statements, it has to be `leave` instruction
            // todo: use dynamic function to encapsulate all branching
            //il.BeginExceptionBlock(); 

            Loop(il, variable, xEnumerator, yEnumerator, gotoNext);

            //il.BeginFinallyBlock();
            EmitDisposeEnumerators(il, xEnumerator, yEnumerator, gotoNext);

            //il.EndExceptionBlock();

            return il.MarkLabel(gotoNext);
        }

        private void EmitCollectionsSorting(
            ILEmitter il,
            Type enumerableType,
            LocalBuilder xEnumerable,
            LocalBuilder yEnumerable)
        {
            throw new NotImplementedException();
        }

        private static (LocalBuilder xEnumerator, LocalBuilder yEnumerator) EmitLoadEnumerators(
            ILEmitter il,
            EnumerableComparison comparison,
            LocalBuilder xEnumerable,
            LocalBuilder yEnumerable)
        {
            il.LoadLocal(xEnumerable)
              .Call(comparison.GetEnumeratorMethod)
              .Store(comparison.EnumeratorType, LocalX, out var xEnumerator)
              .LoadLocal(yEnumerable)
              .Call(comparison.GetEnumeratorMethod)
              .Store(comparison.EnumeratorType, LocalY, out var yEnumerator);
            return (xEnumerator, yEnumerator);
        }

        private void Loop(
            ILEmitter il,
            IVariable variable,
            LocalBuilder xEnumerator,
            LocalBuilder yEnumerator,
            Label gotoNextMember)
        {
            il.DefineLabel(out var startLoop)
              .MarkLabel(startLoop);

            var (xDone, yDone) = EmitMoveNext(il, xEnumerator, yEnumerator);
            EmitIfLoopIsDone(il, xDone, yDone, gotoNextMember);

            var itemComparison = _converter.CreateEnumerableItemComparison(
                variable.OwnerType,
                xEnumerator,
                yEnumerator);

            itemComparison.LoadVariables(_stackVisitor, il, startLoop);
            itemComparison.Accept(_compareVisitor, il)
                          .EmitReturnNotZero(startLoop);
        }

        private static void EmitIfLoopIsDone(
            ILEmitter il,
            LocalBuilder xDone,
            LocalBuilder yDone,
            Label gotoNextMember)
        {
            il.LoadLocal(xDone)
              .Branch(OpCodes.Brfalse_S, out var checkY)
              .LoadLocal(yDone)
              .Branch(OpCodes.Brfalse_S, out var returnM1)
              .Branch(OpCodes.Br, gotoNextMember)
              .MarkLabel(returnM1)
              .Return(-1)
              .MarkLabel(checkY)
              .LoadLocal(yDone)
              .Branch(OpCodes.Brfalse_S, out var compare)
              .Return(1)
              .MarkLabel(compare);
        }

        private static (LocalBuilder xDone, LocalBuilder yDone) EmitMoveNext(
            ILEmitter il,
            LocalBuilder xEnumerator,
            LocalBuilder yEnumerator)
        {
            il.LoadLocal(xEnumerator)
              .Call(Method.MoveNext)
              .LoadConstant(0)
              .Emit(OpCodes.Ceq)
              .Store(typeof(int), LocalDoneX, out var xDone)
              .LoadLocal(yEnumerator)
              .Call(Method.MoveNext)
              .LoadConstant(0)
              .Emit(OpCodes.Ceq)
              .Store(typeof(int), LocalDoneY, out var yDone);

            return (xDone, yDone);
        }

        private static void EmitDisposeEnumerators(
            ILEmitter il,
            LocalBuilder xEnumerator,
            LocalBuilder yEnumerator,
            Label gotoNextMember)
        {
            il.LoadLocal(xEnumerator)
              .Branch(OpCodes.Brfalse_S, out var check)
              .LoadLocal(xEnumerator)
              .Call(Method.Dispose)
              .MarkLabel(check)
              .LoadLocal(yEnumerator)
              .Branch(OpCodes.Brfalse_S, gotoNextMember)
              .LoadLocal(yEnumerator)
              .Call(Method.Dispose);
        }
    }
}
