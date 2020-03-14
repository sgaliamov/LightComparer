﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Illuminator.Extensions;

namespace ILLightenComparer.Shared
{
    internal sealed class GenericProvider
    {
        private readonly Type _genericInterface;
        private readonly ModuleBuilder _moduleBuilder;
        private readonly GenericTypeBuilder _typeBuilder;
        private readonly ConcurrentDictionary<Type, Lazy<Type>> _comparerTypes =
            new ConcurrentDictionary<Type, Lazy<Type>>();
        private readonly ConcurrentDictionary<Type, Lazy<StaticMethodsInfo>> _methodsInfo =
            new ConcurrentDictionary<Type, Lazy<StaticMethodsInfo>>();

        public GenericProvider(Type genericInterface, GenericTypeBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder;
            _genericInterface = genericInterface;
            _moduleBuilder = AssemblyBuilder
                .DefineDynamicAssembly(new AssemblyName("IL-Lighten-Comparer.dll"), AssemblyBuilderAccess.RunAndCollect)
                .DefineDynamicModule("IL-Lighten-Comparer.module");
        }

        // method info is enough to emit compare on sealed type in IndirectComparison
        public MethodInfo GetStaticMethodInfo(Type type, string name) => DefineStaticMethod(type).GetMethodInfo(name);

        // is used for delayed calls in context
        public MethodInfo GetCompiledStaticMethod(Type type, string name)
        {
            EnsureComparerType(type);

            return _methodsInfo.TryGetValue(type, out var item) && item.Value.IsCompiled(name)
                ? item.Value.GetMethodInfo(name)
                : throw new InvalidOperationException("Compiled method is expected.");
        }

        // exposed compiled type to create instance
        public Type EnsureComparerType(Type objectType)
        {
            var lazy = _comparerTypes.GetOrAdd(
                objectType,
                key => new Lazy<Type>(() => {
                    var info = DefineStaticMethod(key);

                    return _typeBuilder.Build(info);
                }));

            var comparerType = lazy.Value;

            FinalizeStartedBuilds();

            return comparerType;
        }

        private StaticMethodsInfo DefineStaticMethod(Type objectType)
        {
            var lazy = _methodsInfo.GetOrAdd(objectType,
                key => new Lazy<StaticMethodsInfo>(() => {
                    var typedInterface = _genericInterface.MakeGenericType(key);

                    var typeBuilder = _moduleBuilder.DefineType(
                        $"{typedInterface.FullName}.DynamicComparer",
                        typedInterface);

                    var methodBuilders = typedInterface
                        .GetMethods()
                        .Select(method => {
                            var parameterTypes = method.GetParameters().Select(x => x.ParameterType).ToArray();

                            var staticMethodParameterTypes = new[] { typeof(IContext) }
                                .Concat(parameterTypes)
                                .Concat(parameterTypes.Select(_ => typeof(CycleDetectionSet)))
                                .ToArray();

                            return typeBuilder.DefineStaticMethod(
                                method.Name,
                                method.ReturnType,
                                staticMethodParameterTypes);
                        })
                        .ToArray();

                    return new StaticMethodsInfo(key, typedInterface, typeBuilder, methodBuilders);
                }));

            return lazy.Value;
        }

        private void FinalizeStartedBuilds()
        {
            foreach (var item in _methodsInfo.ToDictionary(x => x.Key, x => x.Value.Value)) {
                if (item.Value.AllCompiled()) {
                    continue;
                }

                EnsureComparerType(item.Key);
            }
        }
    }
}
