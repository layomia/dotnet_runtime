// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class ConstructorTests_Metadata : ConstructorTests
    {
        public ConstructorTests_Metadata()
            : this(new StringSerializerWrapper(ConstructorTestsContext_Metadata.Default, (options) => new ConstructorTestsContext_Metadata(options)))
        {
        }

        protected ConstructorTests_Metadata(JsonSerializerWrapperForString serializerWrapper)
            : base(serializerWrapper)
        {
        }

        internal sealed partial class ConstructorTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    //public partial class ConstructorTests_Default : ConstructorTests_Metadata
    //{
    //    public ConstructorTests_Default()
    //        : base(new StringSerializerWrapper(ConstructorTestsContext_Default.Default, (options) => new ConstructorTestsContext_Default(options)))
    //    {
    //    }

    //    internal sealed partial class ConstructorTestsContext_Default : JsonSerializerContext
    //    {
    //    }
    //}
}
