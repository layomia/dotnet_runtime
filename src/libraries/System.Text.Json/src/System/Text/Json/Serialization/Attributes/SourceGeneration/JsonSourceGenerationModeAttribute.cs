// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Determines whether the System.Text.Json source generator generates type-metadata initialization logic which
    /// supports all <see cref="JsonSerializer"/> features, or serialization logic, which supports only a subset of features.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class JsonSourceGenerationModeAttribute : JsonAttribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="JsonSourceGenerationModeAttribute"/> with the specified generation mode.
        /// </summary>
        /// <param name="generationMode">The source generation mode.</param>
        public JsonSourceGenerationModeAttribute(JsonSourceGenerationMode generationMode) { }
    }
}
