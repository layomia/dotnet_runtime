// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public class PropertyVisibilityTests_SourceGenAll : PropertyVisibilityTests
    {
        public PropertyVisibilityTests_SourceGenAll()
            : base(new JsonSerializerSourceGen(PropertyVisibilityTestsContext.Default, (options) => new PropertyVisibilityTestsContext(options)))
        {
        }
    }

    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotField))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithInternalField))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotDecimalField))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotAttributedDecimalField))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredPropertyPolicyConflictPrivate))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithMissingCollectionProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithClassProperty_IgnoreConditionWhenWritingDefault))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNoSetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithStructProperty_IgnoreConditionNever))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithPropertyNamingConflict))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithStructProperty_IgnoreConditionWhenWritingDefault))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithMissingObjectProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithInitOnlyProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.StructWithInitOnlyProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.MyClassWithValueTypeInterfaceProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNonPublicProperties))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithProperty_IgnoreConditionAlways))]
    [JsonSerializable(typeof(PropertyVisibilityTests.Class_PropertyWith_PrivateInitOnlySetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithBadIgnoreAttribute))]
    [JsonSerializable(typeof(PropertyVisibilityTests.StructWithBadIgnoreAttribute))]
    [JsonSerializable(typeof(PropertyVisibilityTests.Class_PropertyWith_InternalInitOnlySetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.Class_PropertyWith_ProtectedInitOnlySetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredPropertyPolicyConflictPublic))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredPropertyNamingConflictPrivate))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredNewSlotProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithPublicGetterAndPrivateSetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithInitializedProps))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNewSlotInternalProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithPropertyPolicyConflict))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithPrivateSetterAndGetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoreAttributeProperty))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithIgnoredNewSlotField))]
    [JsonSerializable(typeof(PropertyVisibilityTests.MyStruct_WithNonPublicAccessors_WithTypeAttribute))]
    [JsonSerializable(typeof(PropertyVisibilityTests.MyStruct_WithNonPublicAccessors_WithTypeAttribute))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithReadOnlyFields))]
    [JsonSerializable(typeof(PropertyVisibilityTests.MyValueTypeWithBoxedPrimitive))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithNoGetter))]
    [JsonSerializable(typeof(PropertyVisibilityTests.ClassWithPropsAndIgnoreAttributes))]
    [JsonSerializable(typeof(List<bool>))]
    [JsonSerializable(typeof(PropertyVisibilityTests.MyValueTypeWithProperties))]
    internal sealed partial class PropertyVisibilityTestsContext : JsonSerializerContext
    {
    }

    //[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
    //internal sealed partial class PropertyVisibilityTestsMetadataContext : JsonSerializerContext
    //{
    //}

    //[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Serialization)]
    //internal sealed partial class PropertyVisibilityTestsSerializationContext : JsonSerializerContext
    //{
    //}
}
