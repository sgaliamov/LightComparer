﻿using System;
using System.Collections.Generic;

namespace ILLightenComparer
{
    /// <summary>
    ///     Interface to build an instance of a comparer based on provided type and configuration.
    /// </summary>
    public interface IComparerBuilder : IComparerProvider
    {
        /// <summary>
        ///     Sugar to convert the builder to generic version.
        /// </summary>
        /// <typeparam name="T">
        ///     The type whose instances need to compare.
        ///     Defines context for the following methods.
        /// </typeparam>
        IComparerBuilder<T> For<T>();

        IComparerBuilder<T> For<T>(Action<IConfigurationBuilder<T>> config);

        IComparerBuilder Configure(Action<IConfigurationBuilder> config);

        IComparerBuilder SetCustomComparer<T>(IComparer<T> instance);

        IComparerBuilder SetCustomComparer<TComparer>();
    }

    /// <summary>
    ///     Interface to build an instance of a comparer based on provided type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The type whose instances need to compare.</typeparam>
    public interface IComparerBuilder<T> : IComparerProvider<T>
    {
        IComparerBuilder Builder { get; }

        /// <summary>
        ///     Sugar to convert the builder to generic version.
        ///     Starts another builder context.
        /// </summary>
        /// <typeparam name="TOther">
        ///     The type whose instances need to compare.
        ///     Defines context for the following methods.
        /// </typeparam>
        IComparerBuilder<TOther> For<TOther>();

        IComparerBuilder<TOther> For<TOther>(Action<IConfigurationBuilder<TOther>> config);

        IComparerBuilder<T> Configure(Action<IConfigurationBuilder<T>> config);
    }

    public interface IConfigurationBuilder
    {
        IConfigurationBuilder DefaultDetectCycles(bool? value);

        IConfigurationBuilder DefaultIgnoreCollectionOrder(bool? value);

        IConfigurationBuilder DefaultIgnoredMembers(string[] value);

        IConfigurationBuilder DefaultIncludeFields(bool? value);

        IConfigurationBuilder DefaultMembersOrder(string[] value);

        IConfigurationBuilder DefaultStringComparisonType(StringComparison? value);

        IConfigurationBuilder DetectCycles(Type type, bool? value);

        IConfigurationBuilder IgnoreCollectionOrder(Type type, bool? value);

        IConfigurationBuilder IgnoredMembers(Type type, string[] value);

        IConfigurationBuilder IncludeFields(Type type, bool? value);

        IConfigurationBuilder MembersOrder(Type type, string[] value);

        IConfigurationBuilder StringComparisonType(Type type, StringComparison? value);

        IConfigurationBuilder<T> Configure<T>(Action<IConfigurationBuilder<T>> config);
    }

    public interface IConfigurationBuilder<out T>
    {
        IConfigurationBuilder<T> DetectCycles(bool? value);

        IConfigurationBuilder<T> IgnoreCollectionOrder(bool? value);

        IConfigurationBuilder<T> IgnoredMembers(string[] value);

        IConfigurationBuilder<T> IncludeFields(bool? value);

        IConfigurationBuilder<T> MembersOrder(string[] value);

        IConfigurationBuilder<T> StringComparisonType(StringComparison? value);
    }

    public interface IComparerProvider
    {
        IComparer<T> GetComparer<T>();

        //IEqualityComparer<T> GetEqualityComparer<T>();
    }

    public interface IComparerProvider<in T>
    {
        IComparer<T> GetComparer();

        IComparer<TOther> GetComparer<TOther>();

        //IEqualityComparer<T> GetEqualityComparer();
    }
}
