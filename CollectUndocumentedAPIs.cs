using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public static class CollectUndocumentedAPIs
{
    [MenuItem("Tools/Collect undocumented APIs")]
    public static void Execute()
    {
        var result = new StringBuilder();
        result.AppendLine($"Unity {Application.unityVersion} undocumented APIs")
            .AppendLine("===")
            .AppendLine()
            .AppendLine("Non-public, interface, abstract, generic, nested, delegate and obsoleted items are excluded.")
            .AppendLine();

        var engineFolder = EditorApplication.applicationPath
            .Remove(EditorApplication.applicationPath.Length - "Editor/Unity.exe".Length);
        var docFolder = $"{engineFolder}Editor/Data/Documentation/en/ScriptReference/";
        if (!Directory.Exists(docFolder))
        {
            result.AppendLine("Failed: Local documentation not exist.")
                .AppendLine();
            SaveResult(result);
            return;
        }

        var typeMemberNameSet = new HashSet<string>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic || !assembly.Location.Contains("Editor\\Data\\Managed"))
            {
                continue;
            }

            var appendAssembly = true;
            foreach (var type in assembly.GetTypes())
            {
                // Ignore nested types and totally obsoleted types
                if (!type.IsPublic || type.IsInterface || type.IsAbstract || type.IsGenericType ||
                    type.BaseType == typeof(MulticastDelegate) ||
                    type.GetCustomAttribute<ObsoleteAttribute>() != null)
                {
                    continue;
                }

                string typeDocUrlNoDotHtml;
                if (type.Namespace?.StartsWith("Unity.") ?? false)
                {
                    typeDocUrlNoDotHtml = $"{docFolder}{type.Namespace}.{type.Name}";
                }
                else
                {
                    var firstDotIndexInNamespace = type.Namespace?.IndexOf('.') ?? -1;
                    if (firstDotIndexInNamespace > -1)
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        var directNamespace = type.Namespace.Substring(firstDotIndexInNamespace + 1);
                        typeDocUrlNoDotHtml = $"{docFolder}{directNamespace}.{type.Name}";
                    }
                    else
                    {
                        typeDocUrlNoDotHtml = $"{docFolder}{type.Name}";
                    }
                }

                var appendType = true;
                if (!File.Exists($"{typeDocUrlNoDotHtml}.html"))
                {
                    if (appendAssembly)
                    {
                        appendAssembly = false;
                        var assemblyPath = assembly.Location.Remove(0, engineFolder.Length)
                            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        result.AppendLine($"- [Assembly] {assemblyPath}");
                    }

                    appendType = false;
                    result.AppendLine($"    - [Type] {type.FullName}");
                    // Debug.Log($"{typeDocUrlNoDotHtml}.html");
                }

                if (type.IsEnum)
                {
                    continue;
                }

                typeMemberNameSet.Clear();
                foreach (var member in type.GetMembers())
                {
                    if (typeMemberNameSet.Contains(member.Name))
                    {
                        continue;
                    }

                    if (member.DeclaringType != type ||
                        (member.MemberType & MemberTypes.Constructor) != 0 ||
                        (member.MemberType & MemberTypes.NestedType) != 0 ||
                        member.Name.StartsWith("get_") ||
                        member.Name.StartsWith("set_") ||
                        member.Name.StartsWith("op_") ||
                        member.Name.StartsWith("add_") ||
                        member.Name.StartsWith("remove_") ||
                        member.Name.StartsWith("Equals") ||
                        member.Name.StartsWith("GetHashCode") ||
                        member.Name.StartsWith("ToString") ||
                        // member.Name.StartsWith("value__") || // For enum type
                        member.GetCustomAttribute<ObsoleteAttribute>() != null)
                    {
                        continue;
                    }

                    if (member is MethodInfo method && method.IsVirtual)
                    {
                        continue;
                    }

                    if (member is PropertyInfo property)
                    {
                        var getMethod = property.GetMethod;
                        var setMethod = property.SetMethod;
                        if ((getMethod != null && getMethod.GetBaseDefinition() != getMethod) ||
                            (setMethod != null && setMethod.GetBaseDefinition() != setMethod))
                        {
                            continue;
                        }
                    }

                    if (!File.Exists($"{typeDocUrlNoDotHtml}.{member.Name}.html") &&
                        !File.Exists($"{typeDocUrlNoDotHtml}-{member.Name}.html"))
                    {
                        typeMemberNameSet.Add(member.Name);

                        if (appendType)
                        {
                            if (appendAssembly)
                            {
                                appendAssembly = false;
                                var assemblyPath = assembly.Location.Remove(0, engineFolder.Length)
                                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                                result.AppendLine($"- [Assembly] {assemblyPath}");
                            }

                            appendType = false;
                            result.AppendLine($"    - [Type] {type.FullName}");
                        }

                        result.AppendLine($"        - [{member.MemberType.ToString()}] {member.Name}");

                        // Debug.Log($"{typeDocUrlNoDotHtml}.{member.Name}.html");
                        // Debug.Log($"{typeDocUrlNoDotHtml}-{member.Name}.html");
                    }
                }
            }
        }

        result.AppendLine();
        SaveResult(result);
    }

    private static void SaveResult(StringBuilder result, [CallerFilePath] string callerFilePath = "")
    {
        var folder = Path.GetDirectoryName(callerFilePath);
        Assert.IsNotNull(folder);
        var fileName = $"Unity_{Application.unityVersion.Replace(' ', '_')}_Undocumented_APIs.md";
        var filePath = Path.Combine(folder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.WriteAllText(filePath, result.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        var resultFilePath = Path.Combine(folder.Substring(Application.dataPath.Length - "Assets".Length), fileName);
        var resultFile = AssetDatabase.LoadAssetAtPath<TextAsset>(resultFilePath);
        Debug.Log($"Save undocumented APIs list to file {filePath}.", resultFile);
    }
}