// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json.Serialization
{
    internal static class IEnumerableConverterFactoryHelpers
    {
        // System.Text.Json doesn't take a direct reference to System.Collections.Immutable so
        // any netstandard2.0 consumers don't need to reference System.Collections.Immutable.
        // So instead, implement a "weak reference" by using strings to check for Immutable types.

        // Immutable collection builder types.
        private const string ImmutableArrayTypeName = "System.Collections.Immutable.ImmutableArray";
        private const string ImmutableListTypeName = "System.Collections.Immutable.ImmutableList";
        private const string ImmutableStackTypeName = "System.Collections.Immutable.ImmutableStack";
        private const string ImmutableQueueTypeName = "System.Collections.Immutable.ImmutableQueue";
        private const string ImmutableSortedSetTypeName = "System.Collections.Immutable.ImmutableSortedSet";
        private const string ImmutableHashSetTypeName = "System.Collections.Immutable.ImmutableHashSet";
        private const string ImmutableDictionaryTypeName = "System.Collections.Immutable.ImmutableDictionary";
        private const string ImmutableSortedDictionaryTypeName = "System.Collections.Immutable.ImmutableSortedDictionary";

        private const string CreateRangeMethodName = "CreateRange";

        // Don't use DynamicDependency attributes to the Immutable Collection types so they can be trimmed in applications that don't use Immutable Collections.
        internal const string ImmutableConvertersUnreferencedCodeMessage = "System.Collections.Immutable converters use Reflection to find and create Immutable Collection types, which requires unreferenced code.";

        internal static Type? GetCompatibleGenericBaseClass(this Type type, Type baseType)
        {
            Debug.Assert(baseType.IsGenericType);
            Debug.Assert(!baseType.IsInterface);
            Debug.Assert(baseType == baseType.GetGenericTypeDefinition());

            Type? baseTypeToCheck = type;

            while (baseTypeToCheck != null && baseTypeToCheck != JsonTypeInfo.ObjectType)
            {
                if (baseTypeToCheck.IsGenericType)
                {
                    Type genericTypeToCheck = baseTypeToCheck.GetGenericTypeDefinition();
                    if (genericTypeToCheck == baseType)
                    {
                        return baseTypeToCheck;
                    }
                }

                baseTypeToCheck = baseTypeToCheck.BaseType;
            }

            return null;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The 'interfaceType' must exist and so trimmer kept it. In which case " +
                "It also kept it on any type which implements it. The below call to GetInterfaces " +
                "may return fewer results when trimmed but it will return the 'interfaceType' " +
                "if the type implemented it, even after trimming.")]
        internal static Type? GetCompatibleGenericInterface(this Type type, Type interfaceType)
        {
            Debug.Assert(interfaceType.IsGenericType);
            Debug.Assert(interfaceType.IsInterface);
            Debug.Assert(interfaceType == interfaceType.GetGenericTypeDefinition());

            Type interfaceToCheck = type;

            if (interfaceToCheck.IsGenericType)
            {
                interfaceToCheck = interfaceToCheck.GetGenericTypeDefinition();
            }

            if (interfaceToCheck == interfaceType)
            {
                return type;
            }

            foreach (Type typeToCheck in type.GetInterfaces())
            {
                if (typeToCheck.IsGenericType)
                {
                    Type genericInterfaceToCheck = typeToCheck.GetGenericTypeDefinition();
                    if (genericInterfaceToCheck == interfaceType)
                    {
                        return typeToCheck;
                    }
                }
            }

            return null;
        }

        [RequiresUnreferencedCode(ImmutableConvertersUnreferencedCodeMessage)]
        public static MethodInfo GetImmutableEnumerableCreateRangeMethod(this Type type, Type elementType)
        {
            Type? constructingType = GetImmutableEnumerableConstructingType(type);
            if (constructingType != null)
            {
                MethodInfo[] constructingTypeMethods = constructingType.GetMethods();
                foreach (MethodInfo method in constructingTypeMethods)
                {
                    if (method.Name == CreateRangeMethodName &&
                        method.GetParameters().Length == 1 &&
                        method.IsGenericMethod &&
                        method.GetGenericArguments().Length == 1)
                    {
                        return method.MakeGenericMethod(elementType);
                    }
                }
            }

            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
            return null!;
        }

        [RequiresUnreferencedCode(ImmutableConvertersUnreferencedCodeMessage)]
        public static MethodInfo GetImmutableDictionaryCreateRangeMethod(this Type type, Type keyType, Type valueType)
        {
            Type? constructingType = GetImmutableDictionaryConstructingType(type);
            if (constructingType != null)
            {
                MethodInfo[] constructingTypeMethods = constructingType.GetMethods();
                foreach (MethodInfo method in constructingTypeMethods)
                {
                    if (method.Name == CreateRangeMethodName &&
                        method.GetParameters().Length == 1 &&
                        method.IsGenericMethod &&
                        method.GetGenericArguments().Length == 2)
                    {
                        return method.MakeGenericMethod(keyType, valueType);
                    }
                }
            }

            ThrowHelper.ThrowNotSupportedException_SerializationNotSupported(type);
            return null!;
        }

        [RequiresUnreferencedCode(ImmutableConvertersUnreferencedCodeMessage)]
        private static Type? GetImmutableEnumerableConstructingType(Type type)
        {
            Debug.Assert(type.IsImmutableEnumerableType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            Type underlyingType = type.GetGenericTypeDefinition();
            string constructingTypeName;

            switch (underlyingType.FullName)
            {
                case ReflectionExtensions.ImmutableArrayGenericTypeName:
                    constructingTypeName = ImmutableArrayTypeName;
                    break;
                case ReflectionExtensions.ImmutableListGenericTypeName:
                case ReflectionExtensions.ImmutableListGenericInterfaceTypeName:
                    constructingTypeName = ImmutableListTypeName;
                    break;
                case ReflectionExtensions.ImmutableStackGenericTypeName:
                case ReflectionExtensions.ImmutableStackGenericInterfaceTypeName:
                    constructingTypeName = ImmutableStackTypeName;
                    break;
                case ReflectionExtensions.ImmutableQueueGenericTypeName:
                case ReflectionExtensions.ImmutableQueueGenericInterfaceTypeName:
                    constructingTypeName = ImmutableQueueTypeName;
                    break;
                case ReflectionExtensions.ImmutableSortedSetGenericTypeName:
                    constructingTypeName = ImmutableSortedSetTypeName;
                    break;
                case ReflectionExtensions.ImmutableHashSetGenericTypeName:
                case ReflectionExtensions.ImmutableSetGenericInterfaceTypeName:
                    constructingTypeName = ImmutableHashSetTypeName;
                    break;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }

            return underlyingType.Assembly.GetType(constructingTypeName);
        }

        [RequiresUnreferencedCode(ImmutableConvertersUnreferencedCodeMessage)]
        private static Type? GetImmutableDictionaryConstructingType(Type type)
        {
            Debug.Assert(type.IsImmutableDictionaryType());

            // Use the generic type definition of the immutable collection to determine
            // an appropriate constructing type, i.e. a type that we can invoke the
            // `CreateRange<T>` method on, which returns the desired immutable collection.
            Type underlyingType = type.GetGenericTypeDefinition();
            string constructingTypeName;

            switch (underlyingType.FullName)
            {
                case ReflectionExtensions.ImmutableDictionaryGenericTypeName:
                case ReflectionExtensions.ImmutableDictionaryGenericInterfaceTypeName:
                    constructingTypeName = ImmutableDictionaryTypeName;
                    break;
                case ReflectionExtensions.ImmutableSortedDictionaryGenericTypeName:
                    constructingTypeName = ImmutableSortedDictionaryTypeName;
                    break;
                default:
                    // We verified that the type is an immutable collection, so the
                    // generic definition is one of the above.
                    return null;
            }

            return underlyingType.Assembly.GetType(constructingTypeName);
        }

        public static bool IsNonGenericStackOrQueue(this Type type)
        {
#if BUILDING_INBOX_LIBRARY
            // Optimize for linking scenarios where mscorlib is trimmed out.
            const string stackTypeName = "System.Collections.Stack, System.Collections.NonGeneric";
            const string queueTypeName = "System.Collections.Queue, System.Collections.NonGeneric";
#else
            const string stackTypeName = "System.Collections.Stack, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            const string queueTypeName = "System.Collections.Queue, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
#endif

            Type? stackType = GetTypeIfExists(stackTypeName);
            if (stackType?.IsAssignableFrom(type) == true)
            {
                return true;
            }

            Type? queueType = GetTypeIfExists(queueTypeName);
            if (queueType?.IsAssignableFrom(type) == true)
            {
                return true;
            }

            return false;
        }

        // This method takes an unannotated string which makes linker reflection analysis lose track of the type we are
        // looking for. This indirection allows the removal of the type if it is not used in the calling application.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:TypeGetType",
            Justification = "This method exists to allow for 'weak references' to the Stack and Queue types. If those types are used in the app, " +
            "they will be preserved by the app and Type.GetType will return them. If those types are not used in the app, we don't want to preserve them here.")]
        private static Type? GetTypeIfExists(string name) => Type.GetType(name, false);
    }
}
