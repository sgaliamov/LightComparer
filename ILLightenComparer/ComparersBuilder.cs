﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ILLightenComparer.Emit;
using ILLightenComparer.Emit.Extensions;

namespace ILLightenComparer
{
    public sealed class ComparersBuilder : IComparersBuilder
    {
        private readonly ConcurrentDictionary<Type, IComparer> _comparers = new ConcurrentDictionary<Type, IComparer>();
        private readonly Context _context;

        public ComparersBuilder()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("ILLightenComparer"),
                AssemblyBuilderAccess.RunAndCollect);

            var moduleBuilder = assembly.DefineDynamicModule("ILLightenComparer.dll");
            _context = new Context(moduleBuilder);
        }

        public IContextBuilder SetDefaultConfiguration(CompareConfiguration configuration)
        {
            _context.SetDefaultConfiguration(configuration);
            return this;
        }

        public IContextBuilder SetConfiguration(Type type, CompareConfiguration configuration)
        {
            _context.SetConfiguration(type, configuration);
            return this;
        }

        public IComparer<T> GetComparer<T>() => (IComparer<T>)GetComparer(typeof(T));

        public IComparer GetComparer(Type objectType) =>
            _comparers.GetOrAdd(
                objectType,
                key => _context.GetComparerType(key).CreateInstance<IComparer>());

        public IEqualityComparer<T> GetEqualityComparer<T>() => throw new NotImplementedException();

        public IEqualityComparer GetEqualityComparer(Type objectType) => throw new NotImplementedException();

        public IContextBuilder<T> For<T>() => new GenericProxy<T>(this);

        private sealed class GenericProxy<T> : IContextBuilder<T>, IComparerProviderOrBuilderContext<T>
        {
            private readonly ComparersBuilder _owner;

            public GenericProxy(ComparersBuilder comparersBuilder) => _owner = comparersBuilder;

            public IContextBuilder<TOther> For<TOther>() => _owner.For<TOther>();

            public IComparerProviderOrBuilderContext<T> SetConfiguration(CompareConfiguration configuration)
            {
                _owner.SetConfiguration(typeof(T), configuration);
                return this;
            }

            public IComparer<T> GetComparer() => _owner.GetComparer<T>();

            public IEqualityComparer<T> GetEqualityComparer() => _owner.GetEqualityComparer<T>();
        }
    }
}
