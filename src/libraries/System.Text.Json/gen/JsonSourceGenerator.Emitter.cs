// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.SourceGeneration.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    public sealed partial class JsonSourceGenerator
    {
        private sealed partial class Emitter
        {
            // Literals in generated source
            private const string RuntimeCustomConverterFetchingMethodName = "GetRuntimeProvidedCustomConverter";
            private const string OptionsInstanceVariableName = "Options";
            private const string PropInitMethodNameSuffix = "PropInit";
            private const string SerializeMethodNameSuffix = "FastPathSerialize";
            private const string CreateValueInfoMethodName = "CreateValueInfo";
            private const string CreateDefaultOptionsMethodName = "CreateDefaultOptions";

            private static AssemblyName _assemblyName = typeof(Emitter).Assembly.GetName();
            private static readonly string s_generatedCodeAttributeSource = $@"
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute(""{_assemblyName.Name}"", ""{_assemblyName.Version}"")]";

            // global::fully.qualified.name for referenced types
            private const string ActionTypeRef = "global::System.Action";
            private const string ArrayTypeRef = "global::System.Array";
            private const string TypeTypeRef = "global::System.Type";
            private const string UnsafeTypeRef = "global::System.CompilerServices.Unsafe";
            private const string NullableTypeRef = "global::System.Nullable";
            private const string IListOfTTypeRef = "global::System.Collections.Generic.IList";
            private const string ListOfTTypeRef = "global::System.Collections.Generic.List";
            private const string DictionaryOfTKeyTValueTypeRef = "global::System.Collections.Generic.Dictionary";
            private const string JsonEncodedTextTypeRef = "global::System.Text.Json.JsonEncodedText";
            private const string JsonNamingPolicyTypeRef = "global::System.Text.Json.JsonNamingPolicy";
            private const string JsonSerializerTypeRef = "global::System.Text.Json.JsonSerializer";
            private const string JsonSerializerOptionsTypeRef = "global::System.Text.Json.JsonSerializerOptions";
            private const string Utf8JsonWriterTypeRef = "global::System.Text.Json.Utf8JsonWriter";
            private const string JsonConverterTypeRef = "global::System.Text.Json.Serialization.JsonConverter";
            private const string JsonConverterFactoryTypeRef = "global::System.Text.Json.Serialization.JsonConverterFactory";
            private const string JsonIgnoreConditionTypeRef = "global::System.Text.Json.Serialization.JsonIgnoreCondition";
            private const string JsonNumberHandlingTypeRef = "global::System.Text.Json.Serialization.JsonNumberHandling";
            private const string JsonSerializerContextTypeRef = "global::System.Text.Json.Serialization.JsonSerializerContext";
            private const string JsonMetadataServicesTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonMetadataServices";
            private const string JsonPropertyInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonPropertyInfo";
            private const string JsonTypeInfoTypeRef = "global::System.Text.Json.Serialization.Metadata.JsonTypeInfo";
            private static string JsonContextDeclarationSource = $"internal partial class JsonContext : {JsonSerializerContextTypeRef}";

            // Diagnostic descriptors
            private const string SystemTextJsonSourceGenerationName = "System.Text.Json.SourceGeneration";

            private static DiagnosticDescriptor TypeNotSupported { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1030",
                title: new LocalizableResourceString(nameof(SR.TypeNotSupportedTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.TypeNotSupportedMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private static DiagnosticDescriptor DuplicateTypeName { get; } = new DiagnosticDescriptor(
                id: "SYSLIB1031",
                title: new LocalizableResourceString(nameof(SR.DuplicateTypeNameTitle), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                messageFormat: new LocalizableResourceString(nameof(SR.DuplicateTypeNameMessageFormat), SR.ResourceManager, typeof(FxResources.System.Text.Json.SourceGeneration.SR)),
                category: SystemTextJsonSourceGenerationName,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

            private readonly GeneratorExecutionContext _executionContext;

            private ContextGenerationSpec _currentContext = new();

            private readonly SourceGenerationSpec _generationSpec = null!;

            public Emitter(in GeneratorExecutionContext executionContext, SourceGenerationSpec generationSpec)
            {
                _executionContext = executionContext;
                _generationSpec = generationSpec;
            }

            public void Emit()
            {
                foreach (ContextGenerationSpec contextGenerationSpec in _generationSpec.ContextGenerationSpecList)
                {
                    _currentContext = contextGenerationSpec;

                    foreach (TypeGenerationSpec typeMetadata in _currentContext.RootSerializableTypes)
                    {
                        GenerateTypeMetadata(typeMetadata);
                    }

                    string contextName = _currentContext.ContextType.Name;

                    // Add root context implementation.
                    AddSource($"{contextName}.g.cs", GetRootJsonContextImplementation(), isRootContextDef: true);

                    // Add GetJsonTypeInfo override implementation.
                    AddSource($"{contextName}.GetJsonTypeInfo.g.cs", GetGetTypeInfoImplementation());

                    // Add property name initialization.
                    AddSource($"{contextName}.PropertyNames.g.cs", GetPropertyNameInitialization());
                }
            }

            private void AddSource(string fileName, string source, bool isRootContextDef = false)
            {
                string? generatedCodeAttributeSource = isRootContextDef ? s_generatedCodeAttributeSource : null;

                string wrappedSource = $@"// <auto-generated/>

namespace {_currentContext.ContextType.Namespace}
{{{generatedCodeAttributeSource}
    {IndentSource(source, numIndentations: 1)}
}}
";

                _executionContext.AddSource(fileName, SourceText.From(wrappedSource, Encoding.UTF8));
            }

            private void GenerateTypeMetadata(TypeGenerationSpec typeMetadata)
            {
                Debug.Assert(typeMetadata != null);

                HashSet<TypeGenerationSpec> typesWithMetadata = _currentContext.TypesWithMetadataGenerated;

                if (typesWithMetadata.Contains(typeMetadata))
                {
                    return;
                }

                typesWithMetadata.Add(typeMetadata);

                string source;

                switch (typeMetadata.ClassType)
                {
                    case ClassType.KnownType:
                        {
                            source = GenerateForTypeWithKnownConverter(typeMetadata);
                        }
                        break;
                    case ClassType.TypeWithDesignTimeProvidedCustomConverter:
                        {
                            source = GenerateForTypeWithUnknownConverter(typeMetadata);
                        }
                        break;
                    case ClassType.Nullable:
                        {
                            source = GenerateForNullable(typeMetadata);

                            GenerateTypeMetadata(typeMetadata.NullableUnderlyingTypeMetadata);
                        }
                        break;
                    case ClassType.Enum:
                        {
                            source = GenerateForEnum(typeMetadata);
                        }
                        break;
                    case ClassType.Enumerable:
                        {
                            source = GenerateForCollection(typeMetadata);

                            GenerateTypeMetadata(typeMetadata.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Dictionary:
                        {
                            source = GenerateForCollection(typeMetadata);

                            GenerateTypeMetadata(typeMetadata.CollectionKeyTypeMetadata);
                            GenerateTypeMetadata(typeMetadata.CollectionValueTypeMetadata);
                        }
                        break;
                    case ClassType.Object:
                        {
                            source = GenerateForObject(typeMetadata);

                            if (typeMetadata.PropertiesMetadata != null)
                            {
                                foreach (PropertyGenerationSpec metadata in typeMetadata.PropertiesMetadata)
                                {
                                    GenerateTypeMetadata(metadata.TypeGenerationSpec);
                                }
                            }
                        }
                        break;
                    case ClassType.TypeUnsupportedBySourceGen:
                        {
                            _executionContext.ReportDiagnostic(
                                Diagnostic.Create(TypeNotSupported, Location.None, new string[] { typeMetadata.TypeRef }));
                            return;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }

                try
                {
                    AddSource($"{_currentContext.ContextType.Name}.{typeMetadata.TypeInfoPropertyName}.g.cs", source);
                }
                catch (ArgumentException)
                {
                    _executionContext.ReportDiagnostic(Diagnostic.Create(DuplicateTypeName, Location.None, new string[] { typeMetadata.TypeInfoPropertyName }));
                }
            }

            private string GenerateForTypeWithKnownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $@"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, {JsonMetadataServicesTypeRef}.{typeFriendlyName}Converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForTypeWithUnknownConverter(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                StringBuilder sb = new();

                // TODO (https://github.com/dotnet/runtime/issues/52218): consider moving this verification source to common helper.
                string metadataInitSource = $@"{JsonConverterTypeRef} converter = {typeMetadata.ConverterInstantiationLogic};
                    {TypeTypeRef} typeToConvert = typeof({typeCompilableName});
                    if (!converter.CanConvert(typeToConvert))
                    {{
                        Type underlyingType = {NullableTypeRef}.GetUnderlyingType(typeToConvert);
                        if (underlyingType != null && converter.CanConvert(underlyingType))
                        {{
                            {JsonConverterTypeRef} actualConverter = converter;

                            if (converter is {JsonConverterFactoryTypeRef} converterFactory)
                            {{
                                actualConverter = converterFactory.CreateConverter(underlyingType, {OptionsInstanceVariableName});

                                if (actualConverter == null || actualConverter is {JsonConverterFactoryTypeRef})
                                {{
                                    throw new InvalidOperationException($""JsonConverterFactory '{{converter}} cannot return a 'null' or 'JsonConverterFactory' value."");
                                }}
                            }}

                            // Allow nullable handling to forward to the underlying type's converter.
                            converter = {JsonMetadataServicesTypeRef}.GetNullableConverter<{typeCompilableName}>(({JsonConverterTypeRef}<{typeCompilableName}>)actualConverter);
                        }}
                        else
                        {{
                            throw new InvalidOperationException($""The converter '{{converter.GetType()}}' is not compatible with the type '{{typeToConvert}}'."");
                        }}
                    }}

                    _{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, converter);";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForNullable(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                TypeGenerationSpec? underlyingTypeMetadata = typeMetadata.NullableUnderlyingTypeMetadata;
                Debug.Assert(underlyingTypeMetadata != null);
                string underlyingTypeCompilableName = underlyingTypeMetadata.TypeRef;
                string underlyingTypeFriendlyName = underlyingTypeMetadata.TypeInfoPropertyName;
                string underlyingTypeInfoNamedArg = underlyingTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                    ? "underlyingTypeInfo: null"
                    : $"underlyingTypeInfo: {underlyingTypeFriendlyName}";

                string metadataInitSource = @$"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}(
                        {OptionsInstanceVariableName},
                        {JsonMetadataServicesTypeRef}.GetNullableConverter<{underlyingTypeCompilableName}>({underlyingTypeInfoNamedArg}));
";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForEnum(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string metadataInitSource = $"_{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, {JsonMetadataServicesTypeRef}.GetEnumConverter<{typeCompilableName}>({OptionsInstanceVariableName}));";

                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForCollection(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                // Key metadata
                TypeGenerationSpec? collectionKeyTypeMetadata = typeMetadata.CollectionKeyTypeMetadata;
                Debug.Assert(!(typeMetadata.CollectionType == CollectionType.Dictionary && collectionKeyTypeMetadata == null));
                string? keyTypeCompilableName = collectionKeyTypeMetadata?.TypeRef;
                string? keyTypeReadableName = collectionKeyTypeMetadata?.TypeInfoPropertyName;

                string? keyTypeMetadataPropertyName;
                if (typeMetadata.ClassType != ClassType.Dictionary)
                {
                    keyTypeMetadataPropertyName = "null";
                }
                else
                {
                    keyTypeMetadataPropertyName = collectionKeyTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                        ? "null"
                        : $"this.{keyTypeReadableName}";
                }

                // Value metadata
                TypeGenerationSpec? collectionValueTypeMetadata = typeMetadata.CollectionValueTypeMetadata;
                Debug.Assert(collectionValueTypeMetadata != null);
                string valueTypeCompilableName = collectionValueTypeMetadata.TypeRef;
                string valueTypeReadableName = collectionValueTypeMetadata.TypeInfoPropertyName;

                string valueTypeMetadataPropertyName = collectionValueTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                    ? "null"
                    : $"this.{valueTypeReadableName}";

                string numberHandlingArg = $"{GetNumberHandlingAsStr(typeMetadata.NumberHandling)}";

                CollectionType collectionType = typeMetadata.CollectionType;
                string collectionTypeInfoValue = collectionType switch
                {
                    CollectionType.Array => $"{JsonMetadataServicesTypeRef}.CreateArrayInfo<{valueTypeCompilableName}>({OptionsInstanceVariableName}, {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                    CollectionType.List => $"{JsonMetadataServicesTypeRef}.CreateListInfo<{typeCompilableName}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new {ListOfTTypeRef}<{valueTypeCompilableName}>(), {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                    CollectionType.Dictionary => $"{JsonMetadataServicesTypeRef}.CreateDictionaryInfo<{typeCompilableName}, {keyTypeCompilableName!}, {valueTypeCompilableName}>({OptionsInstanceVariableName}, () => new {DictionaryOfTKeyTValueTypeRef}<{keyTypeCompilableName}, {valueTypeCompilableName}>(), {keyTypeMetadataPropertyName!}, {valueTypeMetadataPropertyName}, {numberHandlingArg})",
                    _ => throw new NotSupportedException()
                };

                string metadataInitSource = @$"_{typeFriendlyName} = {collectionTypeInfoValue};";
                return GenerateForType(typeMetadata, metadataInitSource);
            }

            private string GenerateForObject(TypeGenerationSpec typeMetadata)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;

                string createObjectFuncTypeArg = typeMetadata.ConstructionStrategy == ObjectConstructionStrategy.ParameterlessConstructor
                    ? $"createObjectFunc: static () => new {typeMetadata.TypeRef}()"
                    : "createObjectFunc: null";

                List<PropertyGenerationSpec>? properties = typeMetadata.PropertiesMetadata;

                StringBuilder sb = new();

                sb.Append($@"{JsonTypeInfoTypeRef}<{typeCompilableName}> objectInfo = {JsonMetadataServicesTypeRef}.CreateObjectInfo<{typeCompilableName}>();
                    _{typeFriendlyName} = objectInfo;
");

                string propInitMethodName = $"{typeFriendlyName}{PropInitMethodNameSuffix}";
                string? propMetadataInitFuncSource = null;
                string propMetadataInitFuncNamedArg;

                string serializeMethodName = $"{typeFriendlyName}{SerializeMethodNameSuffix}";
                string? serializeFuncSource = null;
                string serializeFuncNamedArg;

                if (typeMetadata.GenerateMetadata)
                {
                    propMetadataInitFuncSource = GeneratePropMetadataInitFunc(typeMetadata.IsValueType, propInitMethodName, properties);
                    propMetadataInitFuncNamedArg = $@"propInitFunc: {propInitMethodName}";
                }
                else
                {
                    propMetadataInitFuncNamedArg = @"propInitFunc: null";
                }

                if (typeMetadata.GenerateSerializationLogic)
                {
                    serializeFuncSource = GenerateFastPathFuncForObject(typeCompilableName, serializeMethodName, properties);
                    serializeFuncNamedArg = $@"serializeFunc: {serializeMethodName}";
                }
                else
                {
                    serializeFuncNamedArg = @"serializeFunc: null";
                }

                sb.Append($@"
                    {JsonMetadataServicesTypeRef}.InitializeObjectInfo(
                        objectInfo,
                        {OptionsInstanceVariableName},
                        {createObjectFuncTypeArg},
                        {propMetadataInitFuncNamedArg},
                        {serializeFuncNamedArg},
                        {GetNumberHandlingAsStr(typeMetadata.NumberHandling)});");

                string objectInfoInitSource = sb.ToString();

                string additionalSource;
                if (propMetadataInitFuncSource == null || serializeFuncSource == null)
                {
                    additionalSource = propMetadataInitFuncSource ?? serializeFuncSource;
                }
                else
                {
                    additionalSource = @$"{propMetadataInitFuncSource}

    {serializeFuncSource}";
                }

                return GenerateForType(typeMetadata, objectInfoInitSource, additionalSource);
            }

            private string GeneratePropMetadataInitFunc(
                bool declaringTypeIsValueType,
                string propInitMethodName,
                List<PropertyGenerationSpec>? properties)
            {
                const string PropVarName = "properties";
                const string JsonContextVarName = "jsonContext";

                string propertyArrayInstantiationValue = properties == null
                    ? $"{ArrayTypeRef}.Empty<{JsonPropertyInfoTypeRef}>()"
                    : $"new {JsonPropertyInfoTypeRef}[{properties.Count}]";

                string contextTypeRef = _currentContext.ContextTypeRef;

                StringBuilder sb = new();

                sb.Append($@"

    private static {JsonPropertyInfoTypeRef}[] {propInitMethodName}({JsonSerializerContextTypeRef} context)
    {{
        {contextTypeRef} {JsonContextVarName} = ({contextTypeRef})context;
        {JsonSerializerOptionsTypeRef} options = context.Options;

        {JsonPropertyInfoTypeRef}[] {PropVarName} = {propertyArrayInstantiationValue};
");

                if (properties != null)
                {
                    for (int i = 0; i < properties.Count; i++)
                    {
                        PropertyGenerationSpec memberMetadata = properties[i];

                        TypeGenerationSpec memberTypeMetadata = memberMetadata.TypeGenerationSpec;

                        string clrPropertyName = memberMetadata.ClrName;

                        string declaringTypeCompilableName = memberMetadata.DeclaringTypeRef;

                        string memberTypeFriendlyName = memberTypeMetadata.ClassType == ClassType.TypeUnsupportedBySourceGen
                            ? "null"
                            : $"{JsonContextVarName}.{memberTypeMetadata.TypeInfoPropertyName}";

                        string typeTypeInfoNamedArg = $"propertyTypeInfo: {memberTypeFriendlyName}";

                        string jsonPropertyNameNamedArg = memberMetadata.JsonPropertyName != null
                            ? @$"jsonPropertyName: ""{memberMetadata.JsonPropertyName}"""
                            : "jsonPropertyName: null";

                        string getterNamedArg = memberMetadata.CanUseGetter
                            ? $"getter: static (obj) => {{ return (({declaringTypeCompilableName})obj).{clrPropertyName}; }}"
                            : "getter: null";

                        string setterNamedArg;
                        if (memberMetadata.CanUseSetter)
                        {
                            string propMutation = declaringTypeIsValueType
                                ? @$"{{ {UnsafeTypeRef}.Unbox<{declaringTypeCompilableName}>(obj).{clrPropertyName} = value; }}"
                                : $@"{{ (({declaringTypeCompilableName})obj).{clrPropertyName} = value; }}";

                            setterNamedArg = $"setter: static (obj, value) => {propMutation}";
                        }
                        else
                        {
                            setterNamedArg = "setter: null";
                        }

                        JsonIgnoreCondition? ignoreCondition = memberMetadata.DefaultIgnoreCondition;
                        string ignoreConditionNamedArg = ignoreCondition.HasValue
                            ? $"ignoreCondition: JsonIgnoreCondition.{ignoreCondition.Value}"
                            : "ignoreCondition: default";

                        string converterNamedArg = memberMetadata.ConverterInstantiationLogic == null
                            ? "converter: null"
                            : $"converter: {memberMetadata.ConverterInstantiationLogic}";

                        string memberTypeCompilableName = memberTypeMetadata.TypeRef;

                        sb.Append($@"
        {PropVarName}[{i}] = {JsonMetadataServicesTypeRef}.CreatePropertyInfo<{memberTypeCompilableName}>(
            options,
            isProperty: {memberMetadata.IsProperty.ToString().ToLowerInvariant()},
            declaringType: typeof({memberMetadata.DeclaringTypeRef}),
            {typeTypeInfoNamedArg},
            {converterNamedArg},
            {getterNamedArg},
            {setterNamedArg},
            {ignoreConditionNamedArg},
            numberHandling: {GetNumberHandlingAsStr(memberMetadata.NumberHandling)},
            propertyName: ""{clrPropertyName}"",
            {jsonPropertyNameNamedArg});
        ");
                    }
                }

                sb.Append(@$"
        return {PropVarName};
    }}");

                return sb.ToString();
            }

            private string GenerateFastPathFuncForObject(
                string typeInfoTypeRef,
                string serializeMethodName,
                List<PropertyGenerationSpec>? properties)
            {
                const string writerVarName = "writer";
                string methodSignature = $"private static void {serializeMethodName}({Utf8JsonWriterTypeRef} {writerVarName}, {typeInfoTypeRef} value)";

                if (properties == null)
                {
                    return $@"{methodSignature} {{ }}";
                }

                JsonSerializerOptionsAttribute options = _currentContext.SerializerOptions;

                // Add the property names to the context-wide cache; we'll generate the source to initialize them at the end of generation.
                string[] runtimePropNames = GetRuntimePropNames(properties, options.NamingPolicy);
                _currentContext.RuntimePropertyNames.UnionWith(runtimePropNames);

                StringBuilder sb = new();

                // Begin method definition
                sb.Append($@"{methodSignature}
    {{");

                // Provide generation logic for each prop.
                for (int i = 0; i < properties.Count; i++)
                {
                    PropertyGenerationSpec propertySpec = properties[i];

                    if (propertySpec.IsReadOnly)
                    {
                        if (propertySpec.IsProperty)
                        {
                            if (options.IgnoreReadOnlyProperties)
                            {
                                continue;
                            }
                        }
                        else if (options.IgnoreReadOnlyFields)
                        {
                            continue;
                        }
                    }

                    if (!propertySpec.IsProperty && !propertySpec.HasJsonInclude && !options.IncludeFields)
                    {
                        continue;
                    }

                    TypeGenerationSpec propertyTypeSpec = propertySpec.TypeGenerationSpec;
                    Type propertyType = propertyTypeSpec.Type;
                    string propValue = $"value.{propertySpec.ClrName}";
                    string? methodToCall = null;
                    string methodArgs = $"{runtimePropNames[0]}PropName, {propValue}";

                    if (_generationSpec.IsStringBasedType(propertyType))
                    {
                        methodToCall = $"{writerVarName}.WriteString";
                    }
                    else if (propertyType == _generationSpec.BooleanType)
                    {
                        methodToCall = $"{writerVarName}.WriteBoolean";
                    }
                    else if (propertyType == _generationSpec.ByteArrayType)
                    {
                        methodToCall = $"{writerVarName}.WriteBase64String";
                    }
                    else if (propertyType == _generationSpec.CharType)
                    {
                        methodToCall = $"{writerVarName}.WriteBase64String";
                        methodArgs = $"{methodArgs}.ToString()";
                    }
                    else if (_generationSpec.IsNumberType(propertyType))
                    {
                        methodToCall = $"{writerVarName}.WriteNumber";
                    }

                    string propertyTypeInfoPropertyName = $"{propertyTypeSpec.TypeInfoPropertyName}";
                    string propertyTypeInfoRef = $"{_currentContext.ContextTypeRef}.Default.{propertyTypeInfoPropertyName}";
                    string serializationLogic;

                    if (methodToCall != null)
                    {
                        serializationLogic = $@"
        {methodToCall}({methodArgs});";
                    }
                    else
                    {
                        string serializeFuncVarName = $"{propertyTypeInfoPropertyName}SerializeFunc";
                        serializationLogic = $@"
        {ActionTypeRef}<{Utf8JsonWriterTypeRef}, {propertyTypeSpec.TypeRef}> {serializeFuncVarName} = {propertyTypeInfoRef}.Serialize;
        if ({serializeFuncVarName} != null)
        {{
            {serializeFuncVarName}({writerVarName}, {propValue});
        }}
        else
        {{
            {JsonSerializerTypeRef}.Serialize({writerVarName}, {propValue}, {propertyTypeInfoRef});
        }}
";
                    }

                    JsonIgnoreCondition ignoreCondition = propertySpec.DefaultIgnoreCondition ?? options.DefaultIgnoreCondition;
                    DefaultCheckType defaultCheckType;
                    bool typeCanBeNull = propertyTypeSpec.CanBeNull;

                    switch (ignoreCondition)
                    {
                        case JsonIgnoreCondition.WhenWritingNull:
                            defaultCheckType = typeCanBeNull ? DefaultCheckType.Null : DefaultCheckType.None;
                            break;
                        case JsonIgnoreCondition.WhenWritingDefault:
                            defaultCheckType = typeCanBeNull ? DefaultCheckType.Null : DefaultCheckType.Default;
                            break;
                        default:
                            defaultCheckType = DefaultCheckType.None;
                            break;
                    }

                    sb.Append(WrapSerializationLogicInDefaultCheckIfRequired(serializationLogic, propValue, defaultCheckType));
                }

                // End method definition
                sb.Append(@"
    }");

                return sb.ToString();
            }

            private enum DefaultCheckType
            {
                None,
                Null,
                Default,
            }

            private string WrapSerializationLogicInDefaultCheckIfRequired(string serializationLogic, string propValue, DefaultCheckType defaultCheckType)
            {
                if (defaultCheckType == DefaultCheckType.None)
                {
                    return serializationLogic;
                }

                string defaultLiteral = defaultCheckType == DefaultCheckType.Null ? "null" : "default";
                return $@"if ({propValue} != {defaultLiteral})
{{
    {serializationLogic}
}}
";
            }

            private string[] GetRuntimePropNames(List<PropertyGenerationSpec> properties, JsonKnownNamingPolicy namingPolicy)
            {
                int propCount = properties.Count;
                string[] runtimePropNames = new string[propCount];

                // Compute JsonEncodedText values to represent each property name. This gives the best throughput performance
                for (int i = 0; i < propCount; i++)
                {
                    PropertyGenerationSpec propertySpec = properties[i];

                    string propName = DetermineRuntimePropName(propertySpec.ClrName, propertySpec.JsonPropertyName, namingPolicy);
                    Debug.Assert(propName != null);

                    runtimePropNames[i] = propName;
                }

                return runtimePropNames;
            }

            private string DetermineRuntimePropName(string clrPropName, string? jsonPropName, JsonKnownNamingPolicy namingPolicy)
            {
                string runtimePropName;

                if (jsonPropName != null)
                {
                    runtimePropName = jsonPropName;
                }
                else if (namingPolicy == JsonKnownNamingPolicy.BuiltInCamelCase)
                {
                    runtimePropName = JsonNamingPolicy.CamelCase.ConvertName(clrPropName);
                }
                else
                {
                    runtimePropName = clrPropName;
                }

                return runtimePropName;
            }

            private string GenerateForType(TypeGenerationSpec typeMetadata, string metadataInitSource, string? additionalSource = null)
            {
                string typeCompilableName = typeMetadata.TypeRef;
                string typeFriendlyName = typeMetadata.TypeInfoPropertyName;
                string typeInfoPropertyTypeRef = $"{JsonTypeInfoTypeRef}<{typeCompilableName}>";

                return @$"{JsonContextDeclarationSource}
{{
    private {typeInfoPropertyTypeRef} _{typeFriendlyName};
    public {typeInfoPropertyTypeRef} {typeFriendlyName}
    {{
        get
        {{
            if (_{typeFriendlyName} == null)
            {{
                {WrapWithCheckForCustomConverterIfRequired(metadataInitSource, typeCompilableName, typeFriendlyName, GetNumberHandlingAsStr(typeMetadata.NumberHandling))}
            }}

            return _{typeFriendlyName};
        }}
    }}{additionalSource}
}}";
            }

            private string WrapWithCheckForCustomConverterIfRequired(string source, string typeCompilableName, string typeFriendlyName, string numberHandlingNamedArg)
            {
                if (_currentContext.SerializerOptions.IgnoreRuntimeCustomConverters)
                {
                    return source;
                }

                return @$"{JsonConverterTypeRef} customConverter;
                    if ({OptionsInstanceVariableName}.Converters.Count > 0 && (customConverter = {RuntimeCustomConverterFetchingMethodName}(typeof({typeCompilableName}))) != null)
                    {{
                        _{typeFriendlyName} = {JsonMetadataServicesTypeRef}.{GetCreateValueInfoMethodRef(typeCompilableName)}({OptionsInstanceVariableName}, customConverter);
                    }}
                    else
                    {{
                        {IndentSource(source, numIndentations: 1)}
                    }}";
            }

            private string GetRootJsonContextImplementation()
            {
                string contextTypeRef = _currentContext.ContextTypeRef;
                string contextTypeName = _currentContext.ContextType.Name;

                StringBuilder sb = new();

                sb.Append(@$"{JsonContextDeclarationSource}
{{
    private static {contextTypeRef} s_default;
    public static {contextTypeRef} Default => s_default ??= new {contextTypeRef}({CreateDefaultOptionsMethodName}());

    public {contextTypeName}() : base(null)
    {{
    }}

    public {contextTypeName}({JsonSerializerOptionsTypeRef} options) : base(options)
    {{
    }}

    {GetCreationLogicForDefaultSerializerOptions()}

    {GetFetchLogicForRuntimeSpecifiedCustomConverter()}
}}");

                return sb.ToString();
            }

            private string GetFetchLogicForRuntimeSpecifiedCustomConverter()
            {
                if (_currentContext.SerializerOptions.IgnoreRuntimeCustomConverters)
                {
                    return "";
                }

                // TODO (https://github.com/dotnet/runtime/issues/52218): use a dictionary if count > ~15.
                return @$"private {JsonConverterTypeRef} {RuntimeCustomConverterFetchingMethodName}({TypeTypeRef} type)
    {{
        {IListOfTTypeRef}<{JsonConverterTypeRef}> converters = {OptionsInstanceVariableName}.Converters;

        for (int i = 0; i < converters.Count; i++)
        {{
            {JsonConverterTypeRef} converter = converters[i];

            if (converter.CanConvert(type))
            {{
                if (converter is {JsonConverterFactoryTypeRef} factory)
                {{
                    converter = factory.CreateConverter(type, {OptionsInstanceVariableName});
                    if (converter == null || converter is {JsonConverterFactoryTypeRef})
                    {{
                        throw new System.InvalidOperationException($""The converter '{{factory.GetType()}}' cannot return null or a JsonConverterFactory instance."");
                    }}
                }}

                return converter;
            }}
        }}

        return null;
    }}";
            }

            private string GetCreationLogicForDefaultSerializerOptions()
            {
                JsonSerializerOptionsAttribute options = _currentContext.SerializerOptions;

                string? namingPolicyInit = options.NamingPolicy == JsonKnownNamingPolicy.BuiltInCamelCase
                    ? $@"
                PropertyNamingPolicy = {JsonNamingPolicyTypeRef}.CamelCase"
                    : null;

                return $@"private static {JsonSerializerOptionsTypeRef} {CreateDefaultOptionsMethodName}()
        => new {JsonSerializerOptionsTypeRef}()
        {{
            DefaultIgnoreCondition = {JsonIgnoreConditionTypeRef}.{options.DefaultIgnoreCondition},
            IgnoreReadOnlyFields = {options.IgnoreReadOnlyFields.ToString().ToLowerInvariant()},
            IgnoreReadOnlyProperties = {options.IgnoreReadOnlyProperties.ToString().ToLowerInvariant()},
            IncludeFields = {options.IncludeFields.ToString().ToLowerInvariant()},
            WriteIndented = {options.WriteIndented.ToString().ToLowerInvariant()},{namingPolicyInit}
        }};";
            }

            private string GetGetTypeInfoImplementation()
            {
                StringBuilder sb = new();

                sb.Append(@$"{JsonContextDeclarationSource}
{{
    public override {JsonTypeInfoTypeRef} GetTypeInfo({TypeTypeRef} type)
    {{");

                // TODO (https://github.com/dotnet/runtime/issues/52218): Make this Dictionary-lookup-based if root-serializable type count > 64.
                foreach (TypeGenerationSpec metadata in _currentContext.RootSerializableTypes)
                {
                    if (metadata.ClassType != ClassType.TypeUnsupportedBySourceGen)
                    {
                        sb.Append($@"
        if (type == typeof({metadata.TypeRef}))
        {{
            return this.{metadata.TypeInfoPropertyName};
        }}
");
                    }
                }

                sb.Append(@"
        return null!;
    }
}
");

                return sb.ToString();
            }

            private string GetPropertyNameInitialization()
            {
                // Ensure metadata for types has already occured.
                Debug.Assert(!(
                    _currentContext.TypesWithMetadataGenerated.Count == 0
                    && _currentContext.RuntimePropertyNames.Count > 0));

                StringBuilder sb = new();

                sb.Append(@$"{JsonContextDeclarationSource}
{{");

                foreach (string propName in _currentContext.RuntimePropertyNames)
                {
                    sb.Append($@"
    private static {JsonEncodedTextTypeRef} {propName}PropName = {JsonEncodedTextTypeRef}.Encode(""{propName}"");");
                }

                sb.Append(@"
}");

                return sb.ToString();
            }

            private static string IndentSource(string source, int numIndentations)
            {
                Debug.Assert(numIndentations >= 0);
                return source.Replace(Environment.NewLine, $"{Environment.NewLine}{new string(' ', 4 * numIndentations)}"); // 4 spaces per indentation.
            }

            private static string GetNumberHandlingAsStr(JsonNumberHandling? numberHandling) =>
                 numberHandling.HasValue
                    ? $"(JsonNumberHandling){(int)numberHandling.Value}"
                    : "default";

            private static string GetCreateValueInfoMethodRef(string typeCompilableName) => $"{CreateValueInfoMethodName}<{typeCompilableName}>";
        }
    }
}
