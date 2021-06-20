// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.Tests.Serialization
{
    class JsonSerializerDynamic : JsonSerializerWrapper
    {
        public override T Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json);

        public override object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type);

        public override string Serialize<T>(T value) => JsonSerializer.Serialize(value);

        public override string Serialize<T>(T value, JsonSerializerOptions options) => JsonSerializer.Serialize(value, options);
    }
}
