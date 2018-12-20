﻿using System;
using System.Collections;
using System.Collections.Generic;
using ILLightenComparer.Tests.Samples.Comparers;

namespace ILLightenComparer.Tests.Samples
{
    internal static class SampleTypes
    {
        static SampleTypes()
        {
            Types = new Dictionary<Type, IComparer>
            {
                { typeof(sbyte), null },
                { typeof(byte), null },
                { typeof(char), null },
                { typeof(short), null },
                { typeof(ushort), null },
                { typeof(int), null },
                { typeof(long), null },
                { typeof(ulong), null },
                { typeof(float), null },
                { typeof(double), null },
                { typeof(decimal), null },
                { typeof(EnumSmall), null },
                { typeof(EnumBig), null },
                { typeof(string), StringComparer.Ordinal },
                { typeof(SampleComparableObject), null },
                { typeof(SampleComparableChildObject), null },
                { typeof(SampleComparableStruct<EnumSmall>), null },
                {
                    typeof(SampleObject<EnumSmall?>),
                    new SampleObjectComparer<EnumSmall?>(new NullableComparer<EnumSmall>())
                },
                {
                    typeof(SampleStruct<int>),
                    new SampleStructComparer<int>()
                }
            };
        }

        public static IDictionary<Type, IComparer> Types { get; }
    }
}
