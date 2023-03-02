using System;
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
            .AppendLine(
                "Non-public, interface, abstract, generic, nested, delegate and totally obsoleted items are excluded.")
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
                    (type.GetCustomAttribute<ObsoleteAttribute>()?.IsError ?? false))
                {
                    continue;
                }

                string typeDocUrlNoDotHtml;
                var lastDotIndexInNamespace = type.Namespace?.LastIndexOf('.') ?? -1;
                if (lastDotIndexInNamespace > -1)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var directNamespace = type.Namespace.Substring(lastDotIndexInNamespace + 1);
                    typeDocUrlNoDotHtml = $"{docFolder}{directNamespace}.{type.Name}";
                }
                else
                {
                    typeDocUrlNoDotHtml = $"{docFolder}{type.Name}";
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
                }

                if (type.IsEnum)
                {
                    continue;
                }

                foreach (var member in type.GetMembers())
                {
                    // member.MemberType
                    if (member.DeclaringType != type ||
                        (member.MemberType & MemberTypes.Constructor) != 0 ||
                        (member.MemberType & MemberTypes.NestedType) != 0 ||
                        member.Name.StartsWith("get_") ||
                        member.Name.StartsWith("get_") ||
                        member.Name.StartsWith("set_") ||
                        member.Name.StartsWith("op_") ||
                        member.Name.StartsWith("add_") ||
                        member.Name.StartsWith("Equals") ||
                        member.Name.StartsWith("GetHashCode") ||
                        member.Name.StartsWith("ToString") ||
                        // member.Name.StartsWith("value__") || // For enum type
                        (type.GetCustomAttribute<ObsoleteAttribute>()?.IsError ?? false))
                    {
                        continue;
                    }

                    if (!File.Exists($"{typeDocUrlNoDotHtml}.{member.Name}.html") &&
                        !File.Exists($"{typeDocUrlNoDotHtml}-{member.Name}.html"))
                    {
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