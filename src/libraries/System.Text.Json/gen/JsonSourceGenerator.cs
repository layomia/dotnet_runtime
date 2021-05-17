﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.SourceGeneration.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed partial class JsonSourceGenerator : ISourceGenerator
    {
        /// <summary>
        /// Registers a syntax resolver to receive compilation units.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
            //if (!Debugger.IsAttached) { Debugger.Launch(); }

            SyntaxReceiver receiver = (SyntaxReceiver)executionContext.SyntaxReceiver;
            List<ClassDeclarationSyntax>? contextClasses = receiver.ClassDeclarationSyntaxList;
            if (contextClasses == null)
            {
                return;
            }

            Parser parser = new(executionContext.Compilation);
            SourceGenerationSpec? spec = parser.GetGenerationSpec(receiver.ClassDeclarationSyntaxList);
            if (spec != null)
            {
                Emitter emitter = new(executionContext, spec);
                emitter.Emit();
            }
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            public List<ClassDeclarationSyntax>? ClassDeclarationSyntaxList { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax cds)
                {
                    (ClassDeclarationSyntaxList ??= new List<ClassDeclarationSyntax>()).Add(cds);
                }
            }
        }
    }
}
