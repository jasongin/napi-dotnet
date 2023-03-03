using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace NodeApi.Generator;

// This class is packaged with the analyzer, but runs as a separate command-line tool.
#pragma warning disable RS1035 // Do not do file IO in alayzers

// An analyzer bug results in incorrect reports of CA1822 against methods in this class.
#pragma warning disable CA1822 // Mark members as static

/// <summary>
/// Generats TypeScript type definitions for .NET APIs exported to JavaScript.
/// </summary>
/// <remarks>
/// If some specific types or static methods in the assembly are tagged with
/// <see cref="JSExportAttribute"/>, then type definitions are generated for only those items.
/// Otherwise, type definitions are generated for all public APIs in the assembly that are
/// usable by JavaScript.
/// <para />
/// If there is a documentation comments XML file in the same directory as the assembly then
/// the doc-comments will be automatically included with the generated type definitions.
/// </remarks>
public class TypeDefinitionsGenerator : SourceGenerator
{
    private static readonly Regex s_newlineRegex = new("\n *");

    private readonly NullabilityInfoContext _nullabilityContext = new();

    private readonly Assembly _assembly;
    private readonly XDocument? _assemblyDoc;
    private bool _autoCamelCase;
    private bool _emitDisposable;
    private bool _emitCancellation;

    public static void GenerateTypeDefinitions(
        string assemblyFilePath,
        string typeDefinitionsFilePath)
    {
        // Create a metadata load context that includes a resolver for .NET runtime assemblies
        // along with the NodeAPI assembly and the target assembly.

        // TODO: Add an option for additional reference assemblies.

        string[] runtimeAssemblies = Directory.GetFiles(
            RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
        PathAssemblyResolver assemblyResolver = new(runtimeAssemblies.Concat(new[]
        {
            assemblyFilePath,
            typeof(JSExportAttribute).Assembly.Location,
        }));
        using MetadataLoadContext loadContext = new(
            assemblyResolver, typeof(object).Assembly.GetName().Name);

        Assembly assembly = loadContext.LoadFromAssemblyPath(assemblyFilePath);

        XDocument? assemblyDoc = null;
        string? assemblyDocFilePath = Path.ChangeExtension(assemblyFilePath, ".xml");
        if (File.Exists(assemblyDocFilePath))
        {
            assemblyDoc = XDocument.Load(assemblyDocFilePath);
        }

        TypeDefinitionsGenerator generator = new(assembly, assemblyDoc);
        SourceText generatedSource = generator.GenerateTypeDefinitions();

        File.WriteAllText(typeDefinitionsFilePath, generatedSource.ToString());
    }

    public TypeDefinitionsGenerator(Assembly assembly, XDocument? assemblyDoc)
    {
        _assembly = assembly;
        _assemblyDoc = assemblyDoc;
    }

    public SourceText GenerateTypeDefinitions()
    {
        var s = new SourceBuilder();

        s += "// Generated type definitions for .NET module";

        bool exportAll = !AreAnyItemsExported();
        _autoCamelCase = !exportAll;

        foreach (Type type in _assembly.GetTypes().Where((t) => t.IsPublic))
        {
            if (exportAll || IsTypeExported(type))
            {
                ExportType(ref s, type);
            }
            else
            {
                foreach (MemberInfo member in type.GetMembers(
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static))
                {
                    if (IsMemberExported(member))
                    {
                        ExportMember(ref s, member);
                    }
                }
            }
        }

        GenerateSupportingInterfaces(ref s);

        return s;
    }

    private static bool IsTypeExported(Type type)
    {
        return type.GetCustomAttributesData().Any((a) =>
            a.AttributeType.FullName == typeof(JSModuleAttribute).FullName ||
            a.AttributeType.FullName == typeof(JSExportAttribute).FullName);
    }

    private static bool IsMemberExported(MemberInfo member)
    {
        return member.GetCustomAttributesData().Any((a) =>
            a.AttributeType.FullName == typeof(JSExportAttribute).FullName);
    }

    private static bool IsCustomModuleInitMethod(MemberInfo member)
    {
        return member is MethodInfo && member.GetCustomAttributesData().Any((a) =>
            a.AttributeType.FullName == typeof(JSModuleAttribute).FullName);
    }

    private bool AreAnyItemsExported()
    {
        foreach (Type type in _assembly.GetTypes().Where((t) => t.IsPublic))
        {
            if (IsTypeExported(type))
            {
                return true;
            }
            else
            {
                foreach (MemberInfo member in type.GetMembers(
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static))
                {
                    if (IsMemberExported(member))
                    {
                        return true;
                    }
                    else if (IsCustomModuleInitMethod(member))
                    {
                        throw new InvalidOperationException(
                            "Cannot generate type definitions for an assembly with a " +
                            "custom [JSModule] initialization method.");
                    }
                }
            }
        }

        return false;
    }

    private void ExportType(ref SourceBuilder s, Type type)
    {
        if (type.IsClass || type.IsInterface ||
            (type.IsValueType && !type.IsEnum))
        {
            GenerateClassDefinition(ref s, type);
        }
        else if (type.IsEnum)
        {
            GenerateEnumDefinition(ref s, type);
        }
        else
        {
        }
    }

    private void ExportMember(ref SourceBuilder s, MemberInfo member)
    {
        if (member is MethodInfo method)
        {
            s++;
            GenerateDocComments(ref s, method);
            string exportName = GetExportName(method);
            string parameters = GetTSParameters(method.GetParameters());
            string returnType = GetTSType(method.ReturnParameter);
            s += $"export declare function {exportName}({parameters}): {returnType};";
        }
        else if (member is PropertyInfo property)
        {
            s++;
            GenerateDocComments(ref s, property);
            string exportName = GetExportName(property);
            string propertyType = GetTSType(property);
            string varKind = property.SetMethod == null ? "const " : "var ";
            s += $"export declare {varKind}{exportName}: {propertyType};";
        }
        else
        {
            // TODO: Events, const fields?
        }
    }

    private void GenerateSupportingInterfaces(ref SourceBuilder s)
    {
        if (_emitCancellation)
        {
            s++;
            s += "export interface CancellationToken {";
            s += "readonly isCancellationRequested: boolean;";
            s += "readonly onCancellationRequested: (listener: (e: any) => any) => IDisposable;";
            s += "}";
        }

        if (_emitDisposable || _emitCancellation)
        {
            s++;
            s += "export interface IDisposable {";
            s += "dispose(): void;";
            s += "}";
        }
    }

    private void GenerateClassDefinition(ref SourceBuilder s, Type type)
    {
        s++;
        GenerateDocComments(ref s, type);
        string classKind = type.IsInterface ? "interface" :
            (type.IsAbstract && type.IsSealed) ? "declare namespace" : "declare class";

        string implements = string.Empty;
        /*
        foreach (INamedTypeSymbol? implemented in exportClass.Interfaces.Where(
            (type) => _exportItems.Contains(type, SymbolEqualityComparer.Default)))
        {
            implements += (implements.Length == 0 ? " implements " : ", ");
            implements += implemented.Name;
        }
        */

        string exportName = GetExportName(type);
        s += $"export {classKind} {exportName}{implements} {{";

        bool isFirstMember = true;
        foreach (MemberInfo member in type.GetMembers(
            BindingFlags.Public | BindingFlags.DeclaredOnly |
            BindingFlags.Static | BindingFlags.Instance))
        {
            string memberName = ToCamelCase(member.Name);

            if (!(type.IsAbstract && type.IsSealed) && member is ConstructorInfo constructor)
            {
                if (isFirstMember) isFirstMember = false; else s++;
                GenerateDocComments(ref s, constructor);
                string parameters = GetTSParameters(constructor.GetParameters());
                s += $"constructor({parameters});";
            }
            else if (member is MethodInfo method && !method.IsSpecialName)
            {
                if (isFirstMember) isFirstMember = false; else s++;
                GenerateDocComments(ref s, method);
                string parameters = GetTSParameters(method.GetParameters());
                string returnType = GetTSType(method.ReturnParameter);

                if (type.IsAbstract && type.IsSealed)
                {
                    s += "export function " +
                        $"{memberName}({parameters}): {returnType};";
                }
                else
                {
                    s += $"{(method.IsStatic ? "static " : "")}{memberName}({parameters}): " +
                        $"{returnType};";
                }
            }
            else if (member is PropertyInfo property)
            {
                if (isFirstMember) isFirstMember = false; else s++;
                GenerateDocComments(ref s, member);
                string propertyType = GetTSType(property);

                if (type.IsAbstract && type.IsSealed)
                {
                    string varKind = property.SetMethod == null ? "const " : "var ";
                    s += $"export {varKind}{memberName}: {propertyType};";
                }
                else
                {
                    bool isStatic = property.GetMethod?.IsStatic ??
                        property.SetMethod?.IsStatic ?? false;
                    string readonlyModifier = property.SetMethod == null ? "readonly " : "";
                    s += $"{(isStatic ? "static " : "")}{readonlyModifier}{memberName}: " +
                        $"{propertyType};";
                }
            }
        }

        s += "}";
    }

    private void GenerateEnumDefinition(ref SourceBuilder s, Type type)
    {
        s++;
        GenerateDocComments(ref s, type);
        string exportName = GetExportName(type);
        s += $"export declare enum {exportName} {{";

        bool isFirstMember = true;
        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (isFirstMember) isFirstMember = false; else s++;
            GenerateDocComments(ref s, field);
            s += $"{field.Name} = {field.GetRawConstantValue()},";
        }

        s += "}";
    }

    private string GetTSType(PropertyInfo property)
        => GetTSType(property.PropertyType, _nullabilityContext.Create(property));

    private string GetTSType(ParameterInfo parameter)
        => GetTSType(parameter.ParameterType, _nullabilityContext.Create(parameter));

    private string GetTSType(Type type, NullabilityInfo nullability)
    {
        string tsType = "unknown";

        string? specialType = type.FullName switch
        {
            "System.Void" => "void",
            "System.Boolean" => "boolean",
            "System.SByte" => "number",
            "System.Int16" => "number",
            "System.Int32" => "number",
            "System.Int64" => "number",
            "System.Byte" => "number",
            "System.UInt16" => "number",
            "System.UInt32" => "number",
            "System.UInt64" => "number",
            "System.Single" => "number",
            "System.Double" => "number",
            "System.String" => "string",
            "System.DateTime" => "Date",
            _ => null,
        };

        if (specialType != null)
        {
            tsType = specialType;
        }
        else if (type.FullName == typeof(JSValue).FullName)
        {
            tsType = "any";
        }
        else if (type.Name == typeof(JSCallbackArgs).FullName)
        {
            tsType = "...any[]";
        }
        else if (type.IsArray)
        {
            Type elementType = type.GetElementType()!;
            tsType = GetTSType(elementType, nullability.ElementType!) + "[]";
        }
        else if (type.IsGenericType)
        {
            string typeDefinitionName = type.GetGenericTypeDefinition().FullName!;
            Type[] typeArguments = type.GetGenericArguments();
            NullabilityInfo[] typeArgumentsNullability = nullability.GenericTypeArguments;
            if (typeDefinitionName == typeof(Nullable<>).FullName)
            {
                tsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]) + " | null";
            }
            else if (typeDefinitionName == "Task")
            {
                tsType = $"Promise<{GetTSType(typeArguments[0], typeArgumentsNullability[0])}>";
            }
            else if (typeDefinitionName == "Memory")
            {
                Type elementType = typeArguments[0];
                tsType = elementType.FullName switch
                {
                    "System.SByte" => "Int8Array",
                    "System.Int16" => "Int16Array",
                    "System.Int32" => "Int32Array",
                    "System.Int64" => "BigInt64Array",
                    "System.Byte" => "Uint8Array",
                    "System.UInt16" => "Uint16Array",
                    "System.UInt32" => "Uint32Array",
                    "System.UInt64" => "BigUint64Array",
                    "System.Single" => "Float32Array",
                    "System.Double" => "Float64Array",
                    _ => "unknown",
                };
            }
            else if (typeDefinitionName == typeof(IList<>).FullName)
            {
                tsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]) + "[]";
            }
            else if (typeDefinitionName == typeof(IReadOnlyList<>).FullName)
            {
                tsType = "readonly " + GetTSType(typeArguments[0], typeArgumentsNullability[0]) +
                    "[]";
            }
            else if (typeDefinitionName == typeof(ICollection<>).FullName)
            {
                string elementTsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                return $"Iterable<{elementTsType}> & {{ length: number }}";
            }
            else if (typeDefinitionName == typeof(IReadOnlyCollection<>).FullName)
            {
                string elementTsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                return $"Iterable<{elementTsType}> & {{ length: number, " +
                    $"add(item: {elementTsType}): void, delete(item: {elementTsType}): boolean }}";
            }
            else if (typeDefinitionName == typeof(ISet<>).FullName)
            {
                string elementTsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                return $"Set<{elementTsType}>";
            }
            else if (typeDefinitionName == typeof(IReadOnlySet<>).FullName)
            {
                string elementTsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                return $"ReadonlySet<{elementTsType}>";
            }
            else if (typeDefinitionName == typeof(IEnumerable<>).FullName)
            {
                string elementTsType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                return $"Iterable<{elementTsType}>";
            }
            else if (typeDefinitionName == typeof(IDictionary<,>).FullName)
            {
                string keyTSType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                string valueTSType = GetTSType(typeArguments[1], typeArgumentsNullability[1]);
                tsType = $"Map<{keyTSType}, {valueTSType}>";
            }
            else if (typeDefinitionName == typeof(IReadOnlyDictionary<,>).FullName)
            {
                string keyTSType = GetTSType(typeArguments[0], typeArgumentsNullability[0]);
                string valueTSType = GetTSType(typeArguments[1], typeArgumentsNullability[1]);
                tsType = $"ReadonlyMap<{keyTSType}, {valueTSType}>";
            }
        }
        else if (type.Name == "Task")
        {
            tsType = "Promise<void>";
        }
        else if (type.Name == "CancellationToken")
        {
            tsType = type.Name;
            _emitCancellation = true;
        }
        else if (type.Name == "IDisposable")
        {
            tsType = type.Name;
            _emitDisposable = true;
        }
        else if (IsTypeExported(type))
        {
            tsType = type.Name;
        }
        else if (type.Name == "DateTime")
        {
            tsType = "Date";
        }

        if (nullability?.ReadState == NullabilityState.Nullable &&
            tsType != "any" && !tsType.EndsWith(" | null"))
        {
            tsType += " | null";
        }

        return tsType;
    }

    private string GetTSParameters(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0)
        {
            return string.Empty;
        }
        else if (parameters.Length == 1)
        {
            string parameterType = GetTSType(parameters[0]);
            if (parameterType.StartsWith("..."))
            {
                return $"...{parameters[0].Name}: {parameterType.Substring(3)}";
            }
            else
            {
                return $"{parameters[0].Name}: {parameterType}";
            }
        }

        var s = new StringBuilder();
        s.AppendLine();

        foreach (ParameterInfo p in parameters)
        {
            string parameterType = GetTSType(p);
            s.AppendLine($"{p.Name}: {parameterType},");
        }

        return s.ToString();
    }

    private string GetExportName(MemberInfo member)
    {
        CustomAttributeData? attribute = member.GetCustomAttributesData().FirstOrDefault(
            (a) => a.AttributeType.FullName == typeof(JSExportAttribute).FullName);
        if (attribute != null && attribute.ConstructorArguments.Count > 0 &&
            !string.IsNullOrEmpty(attribute.ConstructorArguments[0].Value as string))
        {
            return (string)attribute.ConstructorArguments[0].Value!;
        }
        else
        {
            return _autoCamelCase && member is not Type ? ToCamelCase(member.Name) : member.Name;
        }
    }

    private void GenerateDocComments(ref SourceBuilder s, MemberInfo member)
    {
        string memberDocName = member switch
        {
            Type type => $"T:{type.FullName}",
            PropertyInfo property => $"P:{property.DeclaringType!.FullName}.{property.Name}",
            MethodInfo method => $"M:{method.DeclaringType!.FullName}.{method.Name}(" +
                string.Join(", ", method.GetParameters().Select((p) => p.ParameterType.FullName)) +
                ")",
            _ => string.Empty,
        };

        XElement? memberElement = _assemblyDoc?.Root?.Element("members")?.Elements("member")
            .FirstOrDefault((m) => m.Attribute("name")?.Value == memberDocName);

        XElement? summaryElement = memberElement?.Element("summary");
        XElement? remarksElement = memberElement?.Element("remarks");
        if (memberElement == null || summaryElement == null ||
            string.IsNullOrWhiteSpace(summaryElement.Value))
        {
            return;
        }

        string summary = s_newlineRegex.Replace(
            summaryElement.Value.Replace("\r", "").Trim(), " ");
        string remarks = s_newlineRegex.Replace(
            (remarksElement?.Value ?? string.Empty).Replace("\r", "").Trim(), " ");

        s += "/**";

        foreach (string commentLine in WrapComment(summary, 90 - 3 - s.Indent.Length))
        {
            s += " * " + commentLine;
        }

        if (!string.IsNullOrEmpty(remarks))
        {
            s += " *";
            foreach (string commentLine in WrapComment(remarks, 90 - 3 - s.Indent.Length))
            {
                s += " * " + commentLine;
            }
        }

        s += " */";
    }
}
