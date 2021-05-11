// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration
{
    internal struct GenerationSpecification
    {
        public Dictionary<string, TypeMetadata>? RootSerializableTypes;
        public JsonSourceGenerationMode GenerationMode;
        public JsonSerializerOptionsAttribute? SerializerOptions;
    }
}
