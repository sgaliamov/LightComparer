﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using ILLightenComparer.Comparer;
using ILLightenComparer.Config;
using ILLightenComparer.Equality;

[assembly: InternalsVisibleTo("IL-Lighten-Comparer")]

namespace ILLightenComparer
{
    /// <summary>
    ///     Implements the API to configure and generate comparers.
    /// </summary>
    public sealed class ComparerBuilder : IComparerBuilder
    {
        private readonly ConfigurationProvider _configurationProvider = new ConfigurationProvider();
        private Lazy<(ComparerContext, EqualityContext)> _contexts;

        public ComparerBuilder() => InitContext();

        public ComparerBuilder(Action<IConfigurationBuilder> config) : this() => Configure(config);

        public IComparer<T> GetComparer<T>() => _contexts.Value.Item1.GetComparer<T>();

        public IEqualityComparer<T> GetEqualityComparer<T>() => _contexts.Value.Item2.GetEqualityComparer<T>();

        public IComparerBuilder<T> For<T>() => new Proxy<T>(this);

        public IComparerBuilder<T> For<T>(Action<IConfigurationBuilder<T>> config) => new Proxy<T>(this).Configure(config);

        public IComparerBuilder Configure(Action<IConfigurationBuilder> config)
        {
            config(_configurationProvider);

            InitContext();

            return this;
        }

        private void InitContext()
        {
            _contexts = new Lazy<(ComparerContext, EqualityContext)>(() => {
                var copyConfiguration = new ConfigurationProvider(_configurationProvider);
                return (
                    new ComparerContext(copyConfiguration),
                    new EqualityContext(copyConfiguration)
                );
            }, LazyThreadSafetyMode.PublicationOnly);
        }

        private sealed class Proxy<T> : IComparerBuilder<T>
        {
            private readonly ComparerBuilder _subject;

            public Proxy(ComparerBuilder subject) => _subject = subject;

            public IComparerBuilder<TOther> For<TOther>() => _subject.For<TOther>();

            public IComparerBuilder<TOther> For<TOther>(Action<IConfigurationBuilder<TOther>> config) => _subject.For(config);

            public IComparerBuilder<T> Configure(Action<IConfigurationBuilder<T>> config)
            {
                _subject._configurationProvider.ConfigureFor(config);

                _subject.InitContext(); // todo: 3. test init order

                return this;
            }

            public IComparerBuilder Builder => _subject;

            public IComparer<T> GetComparer() => _subject.GetComparer<T>();

            public IComparer<TOther> GetComparer<TOther>() => _subject.GetComparer<TOther>();

            public IEqualityComparer<T> GetEqualityComparer() => _subject.GetEqualityComparer<T>();

            public IEqualityComparer<TOther> GetEqualityComparer<TOther>() => _subject.GetEqualityComparer<TOther>();
        }
    }
}
