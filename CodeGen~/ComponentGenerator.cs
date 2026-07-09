// #define UNING_CODEGEN_LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

// ReSharper disable InvocationIsSkipped

namespace Armoury.UniNg.CodeGen
{
    [Generator]
    public sealed class ComponentGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor GenerationFailedDescriptor = new(
            id: "UNINGGEN001",
            title: "UniNg source generation failed",
            messageFormat: "{0}",
            category: "UniNg.CodeGen",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true
        );
        
        private static readonly DiagnosticDescriptor MissingIEquatableWarning = new(
            id: "UNINGGEN002",
            title: "Value input should implement IEquatable<T>",
            messageFormat:
                "Input '{0}' uses custom value type '{1}', but '{1}' does not implement IEquatable<{1}>. " +
                "Generated equality checks may be slower or use less precise equality semantics.",
            category: "UniNg",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        private const string RootNameSpace = "Armoury.UI";
        private const string ComponentBaseMetadataName = "Armoury.UI.Component";
        private const string ComponentMetadataFullyQualifiedName = "global::Armoury.UI.ComponentMetadata";
        
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new Receiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                ExecuteCore(context);
            }
            catch (Exception ex)
            {
                FileLog(context, "GENERATOR FATAL ERROR:");
                FileLog(context, ex.ToString());

                context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "UNINGGEN_FATAL",
                        title: "UniNg generator failed",
                        messageFormat: "UniNg generator failed: {0}",
                        category: "UniNg",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true
                    ),
                    Location.None,
                    ex.ToString()
                ));
            }
        }

        public void ExecuteCore(GeneratorExecutionContext context)
        {
            FileLog(context, $"Process: {Process.GetCurrentProcess().ProcessName}");
            FileLog(context, $"Compilation assembly: {context.Compilation.AssemblyName}");
            
            if (context.SyntaxReceiver is not Receiver receiver)
                return;
            
            foreach (var message in receiver.DebugMessages)
            {
                FileLog(context, message.Message);
            }

            FileLog(context, $"Total candidates for {context.Compilation.AssemblyName}: {receiver.Candidates.Count}.");

            var componentBaseSymbol = context.Compilation.GetTypeByMetadataName(ComponentBaseMetadataName);
            if (componentBaseSymbol == null)
                return;
            
            var unityObjectSymbol = context.Compilation.GetTypeByMetadataName("UnityEngine.Object");

            if (unityObjectSymbol == null)
            {
                ReportError(context, "Could not resolve UnityEngine.Object!");
                return;
            }

            foreach (var classDeclaration in receiver.Candidates)
            {
                var semanticModel = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);

                if (semanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                {
                    FileLog(context, "Ignoring, because it's not an INamedTypeSymbol");
                    continue;
                }

                if (classSymbol.IsAbstract)
                {
                    FileLog(context, $"Ignoring {classSymbol.Name}, because it's abstract");
                    continue;
                }

                if (classSymbol.TypeKind != TypeKind.Class)
                {
                    FileLog(context, $"Ignoring {classSymbol.Name}, because it's not a class");
                    continue;
                }

                if (!InheritsFrom(classSymbol, componentBaseSymbol))
                {
                    FileLog(context, $"Ignoring {classSymbol.Name}, because it doesn't inherit from Component");
                    continue;
                }

                if (!IsPartial(classDeclaration))
                {
                    FileLog(context, $"Ignoring {classSymbol.Name}, because it's not a partial class");
                    // TODO: Add diagnostic
                    continue;
                }

                if (classSymbol.TypeParameters.Length > 0)
                {
                    FileLog(context, $"Ignoring {classSymbol.Name}, because it has type parameters");
                    // TODO: Add diagnostic
                    continue;
                }

                var source = GenerateProviderSource(context, classSymbol, unityObjectSymbol);
                var hintName = $"{classSymbol.ToDisplayString().Replace('.', '_')}.UniNg.g.cs";

                try
                {
                    FileLog(context, $"Found component: {classSymbol.ToDisplayString()}");
                    FileLog(context, $"Containing assembly: {classSymbol.ContainingAssembly.Name}");
                    FileLog(context, $"Compilation assembly: {context.Compilation.AssemblyName}");
                    context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
                    FileLog(context, $"AddSource OK: {hintName}");
                    // FileLog(context, source);
                }
                catch (Exception ex)
                {
                    FileLog(context, $"AddSource FAILED: {hintName}");
                    FileLog(context, ex.ToString());

                    ReportError(
                        context,
                        $"UniNg source generation failed for '{hintName}': {ex.Message}",
                        classDeclaration.Identifier.GetLocation()
                    );
                }
            }
        }
        
        [Conditional("UNING_CODEGEN_LOG")]
        private static void FileLog(GeneratorExecutionContext context, string message)
        {
            try
            {
                var assemblyName = context.Compilation.AssemblyName ?? "<unknown>";

                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Armoury",
                    "Uning",
                    "CodeGen"
                );

                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "uning-source-generator.log");

                var line =
                    $"[{DateTime.Now:HH:mm:ss.fff}] [{assemblyName}] {message}{Environment.NewLine}";

                File.AppendAllText(logPath, line);
            }
            catch
            {
                // Never let logging crash the source generator.
            }
        }
        
        private static string GenerateProviderSource(
            GeneratorExecutionContext context, 
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol unityObjectSymbol
        )
        {
            var namespaceName = componentSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : componentSymbol.ContainingNamespace.ToDisplayString();

            var accessibility = GetAccessibility(componentSymbol);
            var className = componentSymbol.Name;

            var componentFullName = componentSymbol.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat
            );
            
            var inputAttributeSymbol = context.Compilation.GetTypeByMetadataName($"{RootNameSpace}.InputAttribute");
            if (inputAttributeSymbol == null)
            {
                ReportError(context, "Input Attribute Symbol couldn't be computed");
                return string.Empty;
            }
            
            var bindingAttributeSymbol = context.Compilation.GetTypeByMetadataName($"{RootNameSpace}.BindingAttribute");
            if (bindingAttributeSymbol == null)
            {
                ReportError(context, "Binding Attribute Symbol couldn't be computed");
                return string.Empty;
            }
            
            var inputs = GetMembersByAttribute(context, unityObjectSymbol, componentSymbol, inputAttributeSymbol);
            var parentBindings = GetMembersByAttribute(context, unityObjectSymbol, componentSymbol, bindingAttributeSymbol);

            var source = $$"""
                // <auto-generated />
                // This file is generated by UniNg. Do not edit manually.

                using UnityEngine;
                using UnityEngine.UIElements;
                using {{RootNameSpace}};
                using {{RootNameSpace}}.Markers;
                using {{RootNameSpace}}.Markers.Elemental;
                
                {{GenerateNamespaceStart(namespaceName)}}
                    {{""/*[UxmlElement] public partial class {{className}}Marker : ComponentMarker<{{className}}> { }*/}}
                    
                    {{accessibility}} partial class {{className}} : IInputReceiver, IParentBindingSource
                    {
                        {{GenerateInputs(inputs, componentSymbol, inputAttributeSymbol, unityObjectSymbol)}}
                        {{GenerateParentInputBindings(parentBindings, componentSymbol, bindingAttributeSymbol, unityObjectSymbol)}}
                        
                        #if UNITY_EDITOR
                        {{GenerateInputsMetadata(inputs, componentSymbol, inputAttributeSymbol, unityObjectSymbol)}}
                        {{GenerateParentInputBindingsMetadata(parentBindings, componentSymbol, bindingAttributeSymbol, unityObjectSymbol)}}
                        #endif
                        
                        public sealed class Provider : {{ComponentMetadataFullyQualifiedName}}<{{componentFullName}}>.Provider { }
                    }
                {{GenerateNamespaceEnd(namespaceName)}}
                """;

            return source;
        }
            
        private static string GenerateNamespaceStart(string? namespaceName)
        {
            return namespaceName == null
                ? ""
                : $"namespace {namespaceName}\n{{";
        }

        private static string GenerateInputs(
            IReadOnlyList<InputMember> inputs,
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol inputAttributeSymbol,
            INamedTypeSymbol unityObjectSymbol
        )
        {
            return $$"""
                internal static class __InputId
                        {
                            {{
                                string.Join(
                                    "\n",
                                    inputs.Select(input =>
                                        GenerateSingleInputDefinition(input, componentSymbol, inputAttributeSymbol)))
                            }}
                        }
                        
                        internal static class __InputIndex
                        {
                            {{
                                string.Join("\n", inputs.Select((input, index) => $"public const int {input.Name} = {index};"))
                            }}
                        }
                        
                        bool IInputReceiver.SetInput(ulong inputId, in InputValue value, ref InputChangeMask changed)
                        {
                            switch (inputId)
                            {
                            {{
                                string.Join("\n", inputs.Select(input => Indent(
                            GenerateSingleInputReceiver(input, unityObjectSymbol), "    ")))
                            }}
                                default:
                                    return false;
                            }
                        }
                """;

            static string GenerateSingleInputDefinition(
                in InputMember input,
                INamedTypeSymbol componentSymbol,
                INamedTypeSymbol inputAttributeSymbol
            )
                => $"public const ulong {input.Name} = {
                    StableHash.Fnv1A64(GetStableBindingPath(componentSymbol, input, inputAttributeSymbol))
                };";

            static string GenerateSingleInputReceiver(
                in InputMember input,
                INamedTypeSymbol unityObjectSymbol
            )
            {
                var accessorName = GetInputValueAccessorName(input.Type, unityObjectSymbol);
                
                return $"case __InputId.{input.Name}:\n" +
                   Indent($$"""
                     var newValue = value.As{{accessorName}}();
                     if ({{$"!global::System.Collections.Generic.EqualityComparer<{input.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>.Default.Equals({input.Name}, newValue)"}})
                     {
                         {{input.Name}} = newValue;
                         changed.Mark(__InputIndex.{{input.Name}});
                     }
                     return true;
                     """, "                ");
            }
        }

        private static string GenerateParentInputBindings(
            IReadOnlyList<InputMember> parentBindings,
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol bindingAttributeSymbol,
            INamedTypeSymbol unityObjectSymbol
        )
        {
            return $$"""
                internal static class __ParentBindingId
                        {
                            {{
                                string.Join(
                                    "\n",
                                    parentBindings.Select(parentBinding =>
                                        GenerateSingleBindingDefinition(parentBinding, componentSymbol, bindingAttributeSymbol)))
                            }}
                        }
                        
                        bool IParentBindingSource.TryResolveParentBinding(
                            ulong bindingId,
                            out InputValue value
                        )
                        {
                            switch (bindingId)
                            {
                            {{
                                string.Join("\n", parentBindings.Select(parentBinding => Indent(
                                    GenerateSingleInputResolver(parentBinding, unityObjectSymbol), "    ")))
                            }}
                                default:
                                {
                                    value = default;
                                    return false;
                                }
                            }
                        }
                """;
            
            static string GenerateSingleBindingDefinition(
                in InputMember parentBinding,
                INamedTypeSymbol componentSymbol,
                INamedTypeSymbol bindingAttributeSymbol
            )
                => $"public const ulong {parentBinding.Name} = {
                    StableHash.Fnv1A64(GetStableBindingPath(componentSymbol, parentBinding, bindingAttributeSymbol))
                };";
            
            static string GenerateSingleInputResolver(
                in InputMember parentBinding,
                INamedTypeSymbol unityObjectSymbol
            )
            {
                var accessorName = GetInputValueAccessorName(parentBinding.Type, unityObjectSymbol);
                
                return $"case __ParentBindingId.{parentBinding.Name}:\n" +
                       Indent($$"""
                                value = InputValue.From{{accessorName}}({{parentBinding.Name}});
                                return true;
                                """, "                ");
            }
        }

        private static string GenerateInputsMetadata(
            IReadOnlyList<InputMember> inputs,
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol inputAttributeSymbol,
            INamedTypeSymbol unityObjectSymbol
        )
        {
            return $$"""
                [UnityEditor.InitializeOnLoad]
                        internal static class {{componentSymbol.Name}}_InputMetadataRegistration
                        {
                            static {{componentSymbol.Name}}_InputMetadataRegistration()
                            {
                                ComponentInputRegistry.Register(
                                    typeof({{componentSymbol.Name}}),
                                    new InputDescriptor[]
                                    {
                                    {{
                                        string.Join("\n", inputs.Select((input, index) => Indent(
                                            GenerateSingleInputDescriptor(
                                                index, in input, componentSymbol, inputAttributeSymbol, unityObjectSymbol), 
                                            "    ")))
                                    }}
                                    }
                                );
                            }
                        }
                """;

            static string GenerateSingleInputDescriptor(
                int index,
                in InputMember input,
                INamedTypeSymbol componentSymbol,
                INamedTypeSymbol inputAttributeSymbol,
                INamedTypeSymbol unityObjectSymbol
            )
            {
                return $"new InputDescriptor(\n" +
                       Indent($$"""
                                id: {{StableHash.Fnv1A64(GetStableBindingPath(componentSymbol, input, inputAttributeSymbol))}},
                                index: {{index}},
                                memberName: "{{input.Name}}",
                                alias: "{{ToCamelCase(input.Name)}}",
                                valueType: typeof({{input.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}),
                                kind: InputValueKind.{{
                                    GetInputValueKindName(input.Type, unityObjectSymbol)
                                }}),
                                """, "                        ");
            }
        }

        private static string GenerateParentInputBindingsMetadata(
            IReadOnlyList<InputMember> parentBindings,
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol bindingAttributeSymbol,
            INamedTypeSymbol unityObjectSymbol
        )
        {
            return $$"""
                [UnityEditor.InitializeOnLoad]
                        internal static class {{componentSymbol.Name}}_ParentBindingMetadataRegistration
                        {
                            static {{componentSymbol.Name}}_ParentBindingMetadataRegistration()
                            {
                                ParentBindingRegistry.Register(
                                    typeof({{componentSymbol.Name}}),
                                    new ParentBindingDescriptor[]
                                    {
                                    {{
                                        string.Join("\n", parentBindings.Select((parentBinding, index) => Indent(
                                            GenerateSingleBindingDescriptor(
                                                index, in parentBinding, componentSymbol, bindingAttributeSymbol, unityObjectSymbol), 
                                            "    ")))
                                    }}
                                    }
                                );
                            }
                        }
            """;
            
            static string GenerateSingleBindingDescriptor(
                int index,
                in InputMember parentBinding,
                INamedTypeSymbol componentSymbol,
                INamedTypeSymbol bindingAttributeSymbol,
                INamedTypeSymbol unityObjectSymbol
            )
            {
                return $"new ParentBindingDescriptor(\n" +
                        Indent($$"""
                            id: {{componentSymbol.Name}}.__ParentBindingId.{{parentBinding.Name}},
                            path: "{{parentBinding.Name}}",
                            valueType: typeof({{parentBinding.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}),
                            kind: InputValueKind.{{
                                GetInputValueKindName(parentBinding.Type, unityObjectSymbol)
                                }}),
                            """, "                            ");
            }
        }
        
        private static string GetStableBindingPath(
            INamedTypeSymbol componentSymbol,
            in InputMember binding,
            INamedTypeSymbol inputAttributeSymbol)
        {
            var componentName = componentSymbol.ToDisplayString(
                new SymbolDisplayFormat(
                    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
                )
            );

            var alias = GetBindingAlias(binding.Symbol, inputAttributeSymbol)
                        ?? ToCamelCase(binding.Name);

            return $"{componentName}::{alias}";
        }
        
        private static string? GetBindingAlias(
            ISymbol symbol,
            INamedTypeSymbol attributeSymbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass == null)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(
                        attribute.AttributeClass,
                        attributeSymbol))
                {
                    continue;
                }

                if (attribute.ConstructorArguments.Length > 0)
                    return attribute.ConstructorArguments[0].Value as string;

                return null;
            }

            return null;
        }
        
        private static string ToCamelCase(string value)
        {
            if (string.IsNullOrEmpty(value) || char.IsLower(value[0]))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1);
        }
        
        private static IReadOnlyList<InputMember> GetMembersByAttribute(
            GeneratorExecutionContext context,
            INamedTypeSymbol unityObjectSymbol,
            INamedTypeSymbol componentSymbol,
            INamedTypeSymbol attributeSymbol)
        {
            var result = new List<InputMember>();

            foreach (var member in componentSymbol.GetMembers())
            {
                switch (member)
                {
                    case IFieldSymbol field:
                    {
                        if (field.IsStatic)
                            continue;

                        if (!HasAttribute(field, attributeSymbol))
                            continue;

                        result.Add(
                            new InputMember(
                                symbol: field,
                                type: field.Type,
                                name: field.Name,
                                location: field.Locations.FirstOrDefault()
                            )
                        );
                        
                        ReportMissingIEquatableWarningIfNeeded(
                            context,
                            field,
                            field.Type,
                            unityObjectSymbol
                        );

                        break;
                    }

                    case IPropertySymbol property:
                    {
                        if (property.IsStatic)
                            continue;

                        if (property.IsIndexer)
                            continue;

                        if (!HasAttribute(property, attributeSymbol))
                            continue;

                        result.Add(
                            new InputMember(
                                symbol: property,
                                type: property.Type,
                                name: property.Name,
                                location: property.Locations.FirstOrDefault()
                            )
                        );
                        
                        ReportMissingIEquatableWarningIfNeeded(
                            context,
                            property,
                            property.Type,
                            unityObjectSymbol
                        );

                        break;
                    }
                }
            }

            return result;
        }
        
        private static bool HasAttribute(
            ISymbol symbol,
            INamedTypeSymbol attributeSymbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass == null)
                    continue;

                if (InheritsFromOrIs(attribute.AttributeClass, attributeSymbol))
                    return true;
            }

            return false;
        }
        
        private static string GenerateNamespaceEnd(string? namespaceName)
        {
            return namespaceName == null
                ? ""
                : "}";
        }
        
        private static bool InheritsFrom(INamedTypeSymbol symbol, INamedTypeSymbol baseType)
        {
            var current = symbol.BaseType;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static bool IsPartial(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.Modifiers.Any(modifier => modifier.ValueText.Equals("partial"));
        }

        private static string GetAccessibility(INamedTypeSymbol symbol)
        {
            return symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                _ => "internal"
            };
        }
        
        private static void ReportError(
            GeneratorExecutionContext context,
            string message,
            Location? location = null)
        {
            FileLog(context, message);
            
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GenerationFailedDescriptor,
                    location ?? GetFallbackLocation(context),
                    message
                )
            );
        }

        private static Location GetFallbackLocation(GeneratorExecutionContext context)
        {
            var syntaxTree = context.Compilation.SyntaxTrees.FirstOrDefault();

            if (syntaxTree == null)
                return Location.None;

            return Location.Create(
                syntaxTree,
                new TextSpan(0, 0)
            );
        }
        
        private static string Indent(string source, string indentation)
        {
            return string.Join(
                "\n",
                source
                    .Split('\n')
                    .Select(line => line.Length == 0 ? line : indentation + line)
            );
        }
        
        private static string GetInputValueAccessorName(
            ITypeSymbol type,
            INamedTypeSymbol unityObjectSymbol)
        {
            type = UnwrapNullable(type);

            if (InheritsFromOrIs(type, unityObjectSymbol))
            {
                var objectTypeName = type.ToDisplayString(
                    SymbolDisplayFormat.FullyQualifiedFormat
                );

                return $"Object<{objectTypeName}>";
            }

            return type.SpecialType switch
            {
                SpecialType.System_String => "String",
                SpecialType.System_Double => "Double",
                SpecialType.System_Int32 => "Int",
                SpecialType.System_Boolean => "Bool",
                SpecialType.System_Single => "Float",

                _ => $"Value<{type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>"
            };
        }
        
        private static string GetInputValueKindName(
            ITypeSymbol type,
            INamedTypeSymbol unityObjectSymbol)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (unityObjectSymbol == null)
                throw new ArgumentNullException(nameof(unityObjectSymbol));

            type = UnwrapNullable(type);

            if (InheritsFromOrIs(type, unityObjectSymbol))
                return "Object";

            return type.SpecialType switch
            {
                SpecialType.System_String => "String",
                SpecialType.System_Double => "Double",
                SpecialType.System_Int32 => "Int",
                SpecialType.System_Boolean => "Bool",
                SpecialType.System_Single => "Float",

                // Custom structs/classes
                _ => "Value"
            };
        }
        
        private static bool InheritsFromOrIs(
            ITypeSymbol type,
            INamedTypeSymbol baseType)
        {
            if (type is not INamedTypeSymbol namedType)
                return false;

            var current = namedType;

            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType))
                    return true;

                current = current.BaseType;
            }

            return false;
        }

        private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol namedType &&
                namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            return type;
        }

        private sealed class Receiver : ISyntaxReceiver
        {
            public readonly List<ClassDeclarationSyntax> Candidates = new();
            public readonly List<GeneratorDebugMessage> DebugMessages = new();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
                    return;

                DebugMessages.Add(
                    new GeneratorDebugMessage(
                        $"Visited class: {classDeclaration.Identifier.Text}",
                        classDeclaration.Identifier.GetLocation()
                    )
                );

                if (classDeclaration.BaseList == null)
                {
                    DebugMessages.Add(
                        new GeneratorDebugMessage(
                            $"Skipped '{classDeclaration.Identifier.Text}' because it has no base list.",
                            classDeclaration.Identifier.GetLocation()
                        )
                    );

                    return;
                }

                Candidates.Add(classDeclaration);

                DebugMessages.Add(
                    new GeneratorDebugMessage(
                        $"Added candidate: {classDeclaration.Identifier.Text}",
                        classDeclaration.Identifier.GetLocation()
                    )
                );
            }
        }
        
        private readonly struct GeneratorDebugMessage
        {
            public readonly string Message;
            public readonly Location? Location;

            public GeneratorDebugMessage(string message, Location? location)
            {
                Message = message;
                Location = location;
            }
        }
        
        private static void ReportMissingIEquatableWarningIfNeeded(
            GeneratorExecutionContext context,
            ISymbol memberSymbol,
            ITypeSymbol inputType,
            INamedTypeSymbol unityObjectSymbol)
        {
            inputType = UnwrapNullable(inputType);

            // Primitives/string should not warn.
            if (IsBuiltInInputType(inputType))
                return;

            // UnityEngine.Object-derived inputs should not warn.
            if (InheritsFromOrIs(inputType, unityObjectSymbol))
                return;

            // Only warn for normal custom structs/classes.
            if (inputType.TypeKind is not (TypeKind.Struct or TypeKind.Class))
                return;

            if (ImplementsIEquatableOfSelf(inputType, context.Compilation))
                return;

            var typeName = inputType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            context.ReportDiagnostic(Diagnostic.Create(
                MissingIEquatableWarning,
                memberSymbol.Locations.FirstOrDefault(),
                memberSymbol.Name,
                typeName
            ));
        }
        
        private static bool IsBuiltInInputType(ITypeSymbol type)
        {
            return type.SpecialType is
                SpecialType.System_String or
                SpecialType.System_Double or
                SpecialType.System_Int32 or
                SpecialType.System_Boolean or
                SpecialType.System_Single;
        }

        private static bool ImplementsIEquatableOfSelf(
            ITypeSymbol type,
            Compilation compilation)
        {
            var equatableSymbol = compilation.GetTypeByMetadataName("System.IEquatable`1");

            if (equatableSymbol == null)
                return false;

            foreach (var interfaceSymbol in type.AllInterfaces)
            {
                if (!SymbolEqualityComparer.Default.Equals(
                        interfaceSymbol.OriginalDefinition,
                        equatableSymbol))
                {
                    continue;
                }

                if (interfaceSymbol.TypeArguments.Length != 1)
                    continue;

                if (SymbolEqualityComparer.Default.Equals(
                        interfaceSymbol.TypeArguments[0],
                        type))
                {
                    return true;
                }
            }

            return false;
        }
    }
    
    internal abstract class StableHash
    {
        internal static ulong Fnv1A64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            var hash = offset;

            for (var i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= prime;
            }

            return hash;
        }
    }
    
    internal readonly struct InputMember
    {
        public readonly ISymbol Symbol;
        public readonly ITypeSymbol Type;
        public readonly string Name;
        public readonly Location? Location;

        public InputMember(ISymbol symbol, ITypeSymbol type, string name, Location? location)
        {
            Symbol = symbol;
            Type = type;
            Name = name;
            Location = location;
        }
    }
}
