﻿using System;
using System.Collections.Generic;
using System.Reflection;
using ILLightenComparer.Abstractions;
using ILLightenComparer.Config;
using ILLightenComparer.Extensions;
using ILLightenComparer.Shared;
using Illuminator.Extensions;

namespace ILLightenComparer.Equality
{
    internal sealed class EqualityContext : IEqualityComparerContext
    {
        private readonly IConfigurationProvider _configuration;
        private readonly IComparerProvider _comparerProvider;
        private readonly ComparersCollection _emittedComparers = new ComparersCollection();
        private readonly GenericProvider _genericProvider;

        public EqualityContext(IComparerProvider comparerProvider, MembersProvider membersProvider, IConfigurationProvider configuration)
        {
            _configuration = configuration;
            _comparerProvider = comparerProvider;
            var equalityResolver = new EqualityResolver(this, membersProvider, _configuration);
            var hasherResolver = new HasherResolver(this, membersProvider, _configuration);

            var methodEmitters = new Dictionary<string, IStaticMethodEmitter> {
                [nameof(Equals)] = new EqualsStaticMethodEmitter(equalityResolver),
                [nameof(GetHashCode)] = new GetHashCodeStaticMethodEmitter(hasherResolver)
            };

            _genericProvider = new GenericProvider(
                typeof(IEqualityComparer<>),
                new GenericTypeBuilder(methodEmitters, _configuration));
        }

        public bool DelayedEquals<T>(T x, T y, CycleDetectionSet xSet, CycleDetectionSet ySet)
        {
            var comparer = _configuration.GetCustomEqualityComparer<T>();
            if (comparer != null) {
                return comparer.Equals(x, y);
            }

            if (!typeof(T).IsValueType) {
                if (x == null) { return y == null; }
                if (y == null) { return false; }
            }

            var xType = x.GetType();
            var yType = y.GetType();
            if (xType != yType) {
                return false;
            }

            var compareMethod = GetCompiledStaticEqualsMethod(xType);

            return compareMethod.InvokeCompare<IEqualityComparerContext, T, bool>(xType, this, x, y, xSet, ySet);
        }

        public int DelayedHash<T>(T comparable, CycleDetectionSet cycleDetectionSet)
        {
            var comparer = _configuration.GetCustomEqualityComparer<T>();
            if (comparer != null) {
                return comparer.GetHashCode(comparable);
            }

            if (!typeof(T).IsValueType && comparable is null) {
                return 0;
            }

            var actualType = comparable.GetType();

            var hashMethod = GetCompiledStaticHashMethod(actualType);

            return GetHash(hashMethod, actualType, comparable, cycleDetectionSet);
        }

        public MethodInfo GetStaticEqualsMethodInfo(Type type) => _genericProvider.GetStaticMethodInfo(type, nameof(Equals));

        public MethodInfo GetCompiledStaticEqualsMethod(Type type) => _genericProvider.GetCompiledStaticMethod(type, nameof(Equals));

        public MethodInfo GetStaticHashMethodInfo(Type type) => _genericProvider.GetStaticMethodInfo(type, nameof(GetHashCode));

        public MethodInfo GetCompiledStaticHashMethod(Type type) => _genericProvider.GetCompiledStaticMethod(type, nameof(GetHashCode));

        public IEqualityComparer<T> GetEqualityComparer<T>() => _configuration.GetCustomEqualityComparer<T>()
            ?? (IEqualityComparer<T>)_emittedComparers.GetOrAdd(typeof(T), key => CreateInstance<T>(key));

        public IComparer<T> GetComparer<T>() => _comparerProvider.GetComparer<T>();

        private IEqualityComparer<T> CreateInstance<T>(Type key) => _genericProvider
            .EnsureComparerType(key)
            .CreateInstance<IEqualityComparerContext, IEqualityComparer<T>>(this);

        private int GetHash<TComparable>(
            MethodInfo method,
            Type actualType,
            TComparable comparable,
            CycleDetectionSet cycleDetectionSet)
        {
            var isDeclaringTypeMatchedActualMemberType = typeof(TComparable) == actualType;
            if (!isDeclaringTypeMatchedActualMemberType) {
                // todo: 3. refactor after Method.InvokeCompare
                return (int)method.Invoke(
                    null,
                    new object[] { this, comparable, cycleDetectionSet });
            }

            var compare = method.CreateDelegate<Func<IEqualityComparerContext, TComparable, CycleDetectionSet, int>>();

            return compare(this, comparable, cycleDetectionSet);
        }
    }

    internal interface IEqualityComparerContext : IEqualityComparerProvider, IContext, IComparerProvider
    {
        bool DelayedEquals<T>(T x, T y, CycleDetectionSet xSet, CycleDetectionSet ySet);
        int DelayedHash<T>(T x, CycleDetectionSet xSet);
    }
}
