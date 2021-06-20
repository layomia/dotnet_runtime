// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization.Tests
{
    public abstract class JsonSerializerWrapper
    {
        public abstract T Deserialize<T>(string json);

        public abstract T Deserialize<T>(string json, JsonSerializerOptions options);

        public abstract object Deserialize(string json, Type type);

        public abstract object Deserialize(string json, Type type, JsonSerializerOptions options);

        public abstract string Serialize<T>(T value);

        public abstract string Serialize<T>(T value, JsonSerializerOptions options);

        public abstract string Serialize(object value, Type type);
    }
}
