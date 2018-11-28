﻿using System;
using System.Collections;
using System.Collections.Generic;
using ILLightenComparer.Emit.Reflection;

namespace ILLightenComparer.Emit
{
    internal sealed class EqualityComparerTypeBuilder
    {
        private readonly BuilderContext _context;
        private readonly MembersProvider _membersProvider;

        public EqualityComparerTypeBuilder(BuilderContext context, MembersProvider membersProvider)
        {
            _membersProvider = membersProvider;
            _context = context;
        }

        public IEqualityComparer<T> Build<T>() =>
            throw new NotImplementedException();

        internal IEqualityComparer Build(Type type) =>
            throw new NotImplementedException();
    }
}
