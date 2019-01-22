﻿using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using AutoFixture;
using FluentAssertions;
using Force.DeepCloner;
using ILLightenComparer.Tests.Samples;
using ILLightenComparer.Tests.Samples.Comparers;
using ILLightenComparer.Tests.Utilities;
using Xunit;

namespace ILLightenComparer.Tests.ComparerTests
{
    public sealed class SampleComparableTests
    {
        [Fact]
        public void Compare_Comparable_Objects()
        {
            Test(typeof(SampleComparableBaseObject<>), nameof(SampleComparableBaseObject<object>.Comparer), false);
            Test(typeof(SampleComparableChildObject<>), nameof(SampleComparableChildObject<object>.ChildComparer), false);
            Test(typeof(SampleComparableBaseObject<>), nameof(SampleComparableBaseObject<object>.Comparer), true);
            Test(typeof(SampleComparableChildObject<>), nameof(SampleComparableChildObject<object>.ChildComparer), true);

            foreach (var item in SampleTypes.Types)
            {
                typeof(SampleComparableBaseObject<>)
                    .MakeGenericType(item.Key)
                    .GetField(nameof(SampleComparableBaseObject<object>.UsedCompareTo), BindingFlags.Public | BindingFlags.Static)
                    .GetValue(null)
                    .Should()
                    .Be(true);
            }
        }

        [Fact]
        public void Compare_Comparable_Structs()
        {
            Test(typeof(SampleComparableStruct<>), nameof(SampleComparableStruct<object>.Comparer), false);
            Test(typeof(SampleComparableStruct<>), nameof(SampleComparableStruct<object>.Comparer), true);
        }

        [Fact]
        public void Custom_Comparable_Implementation_Should_Return_Negative_When_First_Argument_IsNull()
        {
            var one = new SampleObject<SampleComparableBaseObject<EnumSmall>>
            {
                Property = FixtureBuilder.GetInstance().Create<SampleComparableBaseObject<EnumSmall>>()
            };

            var other = one.DeepClone();
            one.Property = null;

            var comparer = new ComparersBuilder().GetComparer<SampleObject<SampleComparableBaseObject<EnumSmall>>>();

            comparer.Compare(one, other).Should().BeNegative();
        }

        [Fact]
        public void Replaced_Comparable_Object_Is_Compared_With_Custom_Implementation()
        {
            var comparer = new ComparersBuilder().GetComparer<SampleObject<SampleComparableBaseObject<EnumSmall>>>();
            var fixture = FixtureBuilder.GetInstance();

            var one = new SampleObject<SampleComparableBaseObject<EnumSmall>>
            {
                Property = fixture.Create<SampleComparableBaseObject<EnumSmall>>()
            };
            comparer.Compare(one, one.DeepClone()).Should().Be(0);

            for (var i = 0; i < Constants.SmallCount; i++)
            {
                one.Property = fixture.Create<SampleComparableBaseObject<EnumSmall>>();
                var other = new SampleObject<SampleComparableBaseObject<EnumSmall>>
                {
                    Property = fixture.Create<SampleComparableBaseObject<EnumSmall>>()
                };

                var expected = one.Property.CompareTo(other.Property).Normalize();
                var actual = comparer.Compare(one, other).Normalize();

                actual.Should().Be(expected);
            }

            SampleComparableChildObject<EnumSmall>.UsedCompareTo.Should().BeTrue();
        }

        private static void Test(Type comparableGenericType, string comparerName, bool makeNullable)
        {
            foreach (var (key, value) in SampleTypes.Types.Where(x => makeNullable && x.Key.IsValueType || !makeNullable))
            {
                var objectType = key;
                var itemComparer = value;
                if (makeNullable)
                {
                    itemComparer = Helper.CreateNullableComparer(objectType, itemComparer);
                    objectType = objectType.MakeNullable();
                }

                var comparableType = comparableGenericType.MakeGenericType(objectType);
                if (itemComparer != null)
                {
                    comparableType
                        .GetField(comparerName, BindingFlags.Public | BindingFlags.Static)
                        .SetValue(null, itemComparer);
                }

                new GenericTests().GenericTest(comparableType, null, false, Constants.SmallCount);
            }
        }
    }
}
