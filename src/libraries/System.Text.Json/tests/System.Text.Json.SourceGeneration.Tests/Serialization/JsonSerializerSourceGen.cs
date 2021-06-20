// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    internal sealed class JsonSerializerSourceGen : JsonSerializerWrapper
    {
        private readonly JsonSerializerContext _defaultContext;
        private readonly Func<JsonSerializerOptions, JsonSerializerContext> _customContextCreator;

        public JsonSerializerSourceGen(JsonSerializerContext defaultContext, Func<JsonSerializerOptions, JsonSerializerContext> customContextCreator)
        {
            _defaultContext = defaultContext ?? throw new ArgumentNullException(nameof(defaultContext));
            _customContextCreator = customContextCreator ?? throw new ArgumentNullException(nameof(defaultContext));
        }

        public override T Deserialize<T>(string json)
        {
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)_defaultContext.GetTypeInfo(typeof(T));
            return JsonSerializer.Deserialize<T>(json, typeInfo);
        }

        public override T Deserialize<T>(string json, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return JsonSerializer.Deserialize<T>(json, typeInfo);
        }

        public override object Deserialize(string json, Type type) => JsonSerializer.Deserialize(json, type, _defaultContext);
        public override object Deserialize(string json, Type type, JsonSerializerOptions options) => throw new NotImplementedException();

        public override string Serialize<T>(T value)
        {
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)_defaultContext.GetTypeInfo(typeof(T));
            return JsonSerializer.Serialize(value, typeInfo);
        }

        public override string Serialize<T>(T value, JsonSerializerOptions options)
        {
            JsonSerializerContext context = _customContextCreator(new JsonSerializerOptions(options));
            JsonTypeInfo<T> typeInfo = (JsonTypeInfo<T>)context.GetTypeInfo(typeof(T));
            return JsonSerializer.Serialize(value, typeInfo);
        }

        public override string Serialize(object value, Type type) => JsonSerializer.Serialize(value, type, _defaultContext);
    }
}
