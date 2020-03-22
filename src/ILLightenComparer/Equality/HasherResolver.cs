﻿using System;
using System.Collections.Generic;
using System.Linq;
using ILLightenComparer.Config;
using ILLightenComparer.Equality.Hashers;
using ILLightenComparer.Reflection;
using ILLightenComparer.Shared;
using ILLightenComparer.Variables;
using Illuminator.Extensions;

namespace ILLightenComparer.Equality
{
    internal sealed class HasherResolver
    {
        private readonly IReadOnlyCollection<Func<IVariable, IHasherEmitter>> _hashersFactories;
        private readonly IConfigurationProvider _configuration;

        public HasherResolver(
            EqualityContext context,
            MembersProvider membersProvider,
            IConfigurationProvider configuration)
        {
            _configuration = configuration;

            _hashersFactories = new Func<IVariable, IHasherEmitter>[] {
                PrimitiveHasher.Create,
                (IVariable variable) => IndirectHasher.Create(context, variable),
                (IVariable variable) => MembersHasher.Create(this, membersProvider, configuration, variable)
            };
        }

        public IHasherEmitter GetHasher(IVariable variable)
        {
            var hasCustomComparer = _configuration.HasCustomEqualityComparer(variable.VariableType);
            if (hasCustomComparer) {
                return new CustomHasher(variable, Method.DelayedHash);
            }

            var hasher = _hashersFactories
                .Select(factory => factory(variable))
                .FirstOrDefault(x => x != null);

            if (hasher == null) {
                throw new NotSupportedException($"{variable.VariableType.DisplayName()} is not supported.");
            }

            return hasher;
        }
    }
}