﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ILLightenComparer.Extensions;
using ILLightenComparer.Shared;
using Illuminator.Extensions;

namespace ILLightenComparer.Config
{
    using Configurations = ConcurrentDictionary<Type, Configuration>;

    internal interface IConfigurationProvider
    {
        Configuration Get(Type type);
        IComparer<T> GetCustomComparer<T>();
        IEqualityComparer<T> GetCustomEqualityComparer<T>();
        bool HasCustomComparer(Type type);
        bool HasCustomEqualityComparer(Type type);
    }

    internal sealed class ConfigurationProvider : IConfigurationBuilder, IConfigurationProvider
    {
        private const bool IncludeFieldsDefault = true;
        private const StringComparison StringComparisonTypeDefault = StringComparison.Ordinal;
        private const bool DetectCyclesDefault = true;
        private const bool IgnoreCollectionOrderDefault = false;
        private static readonly string[] IgnoredMembersDefault = Array.Empty<string>();
        private static readonly string[] MembersOrderDefault = Array.Empty<string>();

        private readonly Configurations _configurations;
        private readonly ComparersCollection _customComparers;
        private readonly ComparersCollection _customEqualityComparers;

        private readonly Configuration _default = new Configuration(
            IgnoredMembersDefault,
            IncludeFieldsDefault,
            MembersOrderDefault,
            StringComparisonTypeDefault,
            DetectCyclesDefault,
            IgnoreCollectionOrderDefault);

        public ConfigurationProvider()
        {
            _configurations = new Configurations();
            _customComparers = new ComparersCollection();
            _customEqualityComparers = new ComparersCollection();
        }

        public ConfigurationProvider(ConfigurationProvider provider)
        {
            _configurations = new Configurations(provider._configurations.ToDictionary(x => x.Key, x => new Configuration(x.Value)));
            _customComparers = new ComparersCollection(provider._customComparers);
            _customEqualityComparers = new ComparersCollection(provider._customEqualityComparers);
            _default = new Configuration(provider._default);
        }

        public IConfigurationBuilder SetDefaultCyclesDetection(bool? value)
        {
            _default.DetectCycles = value ?? DetectCyclesDefault;

            return this;
        }

        public IConfigurationBuilder SetDefaultCollectionsOrderIgnoring(bool? value)
        {
            _default.IgnoreCollectionOrder = value ?? IgnoreCollectionOrderDefault;

            return this;
        }

        public IConfigurationBuilder SetDefaultFieldsInclusion(bool? value)
        {
            _default.IncludeFields = value ?? IncludeFieldsDefault;

            return this;
        }

        public IConfigurationBuilder SetDefaultStringComparisonType(StringComparison? value)
        {
            _default.StringComparisonType = value ?? StringComparisonTypeDefault;

            return this;
        }

        public IConfigurationBuilder DetectCycles(Type type, bool? value)
        {
            // todo: use default values from _default field, because it can be redefined.
            GetOrCreate(type).DetectCycles = value ?? DetectCyclesDefault;

            return this;
        }

        public IConfigurationBuilder IgnoreCollectionsOrder(Type type, bool? value)
        {
            GetOrCreate(type).IgnoreCollectionOrder = value ?? IgnoreCollectionOrderDefault;

            return this;
        }

        public IConfigurationBuilder IgnoreMember<T, TMember>(Expression<Func<T, TMember>>[] selectors)
        {
            var members = GetMembers(selectors);

            GetOrCreate(typeof(T)).SetIgnoredMembers(members);

            return this;
        }

        public IConfigurationBuilder IncludeFields(Type type, bool? value)
        {
            GetOrCreate(type).IncludeFields = value ?? IncludeFieldsDefault;

            return this;
        }

        public IConfigurationBuilder DefineMembersOrder<T>(Action<IMembersOrder<T>> order)
        {
            if (order == null) {
                GetOrCreate(typeof(T)).SetMembersOrder(MembersOrderDefault);

                return this;
            }

            var members = new MembersOrder<T>();

            order(members);

            GetOrCreate(typeof(T)).SetMembersOrder(members.Order);

            return this;
        }

        public IConfigurationBuilder SetStringComparisonType(Type type, StringComparison? value)
        {
            GetOrCreate(type).StringComparisonType = value ?? StringComparisonTypeDefault;

            return this;
        }

        public IConfigurationBuilder<T> ConfigureFor<T>(Action<IConfigurationBuilder<T>> config)
        {
            var proxy = new Proxy<T>(this);

            config(proxy);

            return proxy;
        }

        public IConfigurationBuilder SetCustomComparer<T>(IComparer<T> instance) =>
            SetCustomComparer(_customComparers, typeof(T), instance);

        public IConfigurationBuilder SetCustomComparer<TComparer>()
        {
            var (type, comparer) = CreateCustomComparer<TComparer>(typeof(IComparer<>));

            return SetCustomComparer(_customComparers, type, comparer);
        }

        public IConfigurationBuilder SetCustomEqualityComparer<T>(IEqualityComparer<T> instance) =>
            SetCustomComparer(_customEqualityComparers, typeof(T), instance);

        public IConfigurationBuilder SetCustomEqualityComparer<TComparer>()
        {
            var (type, comparer) = CreateCustomComparer<TComparer>(typeof(IEqualityComparer<>));

            return SetCustomComparer(_customEqualityComparers, type, comparer);
        }

        public Configuration Get(Type type) =>
            _configurations.TryGetValue(type, out var configuration)
                ? configuration
                : _default;

        public IComparer<T> GetCustomComparer<T>() =>
            _customComparers.TryGetValue(typeof(T), out var comparer) ? (IComparer<T>)comparer : null;

        public IEqualityComparer<T> GetCustomEqualityComparer<T>() =>
            _customEqualityComparers.TryGetValue(typeof(T), out var comparer) ? (IEqualityComparer<T>)comparer : null;

        public bool HasCustomComparer(Type type) => _customComparers.ContainsKey(type);

        public bool HasCustomEqualityComparer(Type type) => _customEqualityComparers.ContainsKey(type);

        private ConfigurationProvider SetCustomComparer(ComparersCollection customComparers, Type objectType, object instance)
        {
            if (instance == null) {
                customComparers.TryRemove(objectType, out _);
            } else {
                customComparers.AddOrUpdate(objectType, _ => instance, (__, _) => instance);
            }

            return this;
        }

        private Configuration GetOrCreate(Type type) => _configurations.GetOrAdd(type, _ => new Configuration(_default));

        private static (Type, TComparer) CreateCustomComparer<TComparer>(Type genericInterface)
        {
            var comparerType = typeof(TComparer);
            var typedGenericInterface = comparerType.FindGenericInterface(genericInterface);
            if (genericInterface == null) {
                throw new ArgumentException($"{nameof(TComparer)} is not generic {genericInterface}");
            }

            var objectType = typedGenericInterface.GenericTypeArguments[0];
            var comparer = comparerType.Create<TComparer>();

            return (objectType, comparer);
        }

        private static string[] GetMembers<T, TMember>(IEnumerable<Expression<Func<T, TMember>>> memberSelectors) => memberSelectors?.Select(GetMemberName).ToArray();

        private static string GetMemberName<T, TMember>(Expression<Func<T, TMember>> selector)
        {
            if (selector.Body.NodeType != ExpressionType.MemberAccess) {
                throw new ArgumentException("Member selector is expected.", nameof(selector));
            }

            var body = (MemberExpression)selector.Body;

            return body.Member.Name;
        }

        private sealed class MembersOrder<T> : IMembersOrder<T>
        {
            private readonly List<string> _order = new List<string>();

            public IEnumerable<string> Order => _order;

            public IMembersOrder<T> Member<TMember>(Expression<Func<T, TMember>> selector)
            {
                _order.Add(GetMemberName(selector));

                return this;
            }
        }

        private sealed class Proxy<T> : IConfigurationBuilder<T>
        {
            private readonly ConfigurationProvider _subject;

            public Proxy(ConfigurationProvider subject) => _subject = subject;

            public IConfigurationBuilder<T> DetectCycles(bool? value)
            {
                _subject.DetectCycles(typeof(T), value);

                return this;
            }

            public IConfigurationBuilder<T> IgnoreCollectionsOrder(bool? value)
            {
                _subject.IgnoreCollectionsOrder(typeof(T), value);

                return this;
            }

            public IConfigurationBuilder<T> IgnoreMember<TMember>(Expression<Func<T, TMember>>[] selectors)
            {
                _subject.IgnoreMember(selectors);

                return this;
            }

            public IConfigurationBuilder<T> IncludeFields(bool? value)
            {
                _subject.IncludeFields(typeof(T), value);

                return this;
            }

            public IConfigurationBuilder<T> DefineMembersOrder(Action<IMembersOrder<T>> order)
            {
                _subject.DefineMembersOrder(order);

                return this;
            }

            public IConfigurationBuilder<T> SetStringComparisonType(StringComparison? value)
            {
                _subject.SetStringComparisonType(typeof(T), value);

                return this;
            }
        }
    }
}
