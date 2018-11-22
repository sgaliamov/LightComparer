﻿using System;
using System.Reflection;
using AutoFixture.Kernel;

namespace ILLightenComparer.Tests.Utilities
{
    internal sealed class NumericGenerator : ISpecimenBuilder
    {
        private readonly int _lower;
        private readonly sbyte _minMaxProbability;
        private readonly Random _random;
        private readonly int _upper;

        public NumericGenerator(int lower, int upper, sbyte minMaxProbability)
        {
            _lower = lower;
            _upper = upper;
            _minMaxProbability = minMaxProbability;
            _random = new Random();
        }

        public object Create(object request, ISpecimenContext context)
        {
            var type = request as Type;
            if (type == null)
            {
                return new NoSpecimen();
            }

            return CreateRandom(type);
        }

        private object CreateRandom(Type request)
        {
            switch (Type.GetTypeCode(request))
            {
                case TypeCode.Byte:
                    return MinMax<byte>(request) ?? (byte)GetNextRandom();

                case TypeCode.Decimal:
                    return MinMax<decimal>(request) ?? GetNextRandom();

                case TypeCode.Double:
                    return MinMax<double>(request) ?? GetNextRandom();

                case TypeCode.Int16:
                    return MinMax<short>(request) ?? (short)GetNextRandom();

                case TypeCode.Int32:
                    return MinMax<int>(request) ?? (int)GetNextRandom();

                case TypeCode.Int64:
                    return MinMax<long>(request) ?? GetNextRandom();

                case TypeCode.SByte:
                    return MinMax<sbyte>(request) ?? (sbyte)GetNextRandom();

                case TypeCode.Single:
                    return MinMax<float>(request) ?? GetNextRandom();

                case TypeCode.UInt16:
                    return MinMax<ushort>(request) ?? (ushort)GetNextRandom();

                case TypeCode.UInt32:
                    return MinMax<uint>(request) ?? (uint)GetNextRandom();

                case TypeCode.UInt64:
                    return MinMax<ulong>(request) ?? (ulong)GetNextRandom();

                default: return new NoSpecimen();
            }
        }

        private T? MinMax<T>(IReflect request)
            where T : struct
        {
            if (!(_random.NextDouble() < _minMaxProbability))
            {
                return null;
            }

            object Get(IReflect t, string name) =>
                t.GetField(name, BindingFlags.Static | BindingFlags.Public).GetValue(null);

            return _random.NextDouble() < 0.5
                ? (T)Get(request, "MinValue")
                : (T)Get(request, "MaxValue");
        }

        private long GetNextRandom() => _random.Next(_lower, _upper);
    }
}
