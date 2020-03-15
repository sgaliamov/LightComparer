﻿using System;
using System.Collections.Generic;

namespace ILLightenComparer.Tests.ComparerTests.Samples
{
    public class SampleComparableBaseObject<TMember> : IComparable<SampleComparableBaseObject<TMember>>
    {
        public static IComparer<TMember> Comparer = Comparer<TMember>.Default;
        public static bool UsedCompareTo;

        public TMember Field;
        public TMember Property { get; set; }

        public virtual int CompareTo(SampleComparableBaseObject<TMember> other)
        {
            UsedCompareTo = true;

            if (ReferenceEquals(this, other)) {
                return 0;
            }

            if (other is null) {
                return 1;
            }

            var compare = Comparer.Compare(Field, other.Field);
            if (compare != 0) {
                return compare;
            }

            return Comparer.Compare(Property, other.Property);
        }

        public override string ToString() => $"{{ {Field}, {Property} }}";
    }
}