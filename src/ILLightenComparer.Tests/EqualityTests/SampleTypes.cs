﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ILLightenComparer.Tests.EqualityComparers;
using ILLightenComparer.Tests.Samples;
using ILLightenComparer.Tests.Utilities;

namespace ILLightenComparer.Tests.EqualityTests
{
    internal static class SampleTypes
    {
        static SampleTypes()
        {
            Types = new Dictionary<Type, IEqualityComparer> {
                [typeof(sbyte)] = null,
                [typeof(byte)] = null,
                [typeof(char)] = null,
                [typeof(short)] = null,
                [typeof(ushort)] = null,
                [typeof(int)] = null,
                [typeof(long)] = null,
                [typeof(ulong)] = null,
                [typeof(float)] = null,
                [typeof(double)] = null,
                [typeof(decimal)] = null,
                [typeof(EnumSmall)] = null,
                [typeof(EnumBig)] = null,
                [typeof(string)] = null,
                [typeof(SampleEqualityBaseObject<EnumSmall?>)] = null,
                [typeof(SampleEqualityChildObject<EnumSmall?>)] = null,
                [typeof(SampleEqualityStruct<EnumSmall?>)] = null,
                [typeof(ComparableObject<EnumSmall?>)] = new ComparableObjectEqualityComparer<EnumSmall?>(),
                [typeof(ComparableStruct<EnumSmall?>)] = new ComparableStructEqualityComparer<EnumSmall?>()
            };

            NullableTypes = Types
                .Where(x => x.Key.IsValueType)
                .ToDictionary(x => x.Key.MakeNullable(), x => Helper.CreateNullableEqualityComparer(x.Key, x.Value));
        }

        public static IDictionary<Type, IEqualityComparer> NullableTypes { get; }

        public static IDictionary<Type, IEqualityComparer> Types { get; }
    }
}
