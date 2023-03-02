using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public static class CollectUndocumentedTypes
{
    [MenuItem("Tools/Collect undocumented types")]
    public static void Execute()
    {
        var result = new StringBuilder();
        result.AppendLine($"Unity {Application.unityVersion} undocumented types")
            .AppendLine("===")
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

                string typeDocUrl;
                var lastDotIndexInNamespace = type.Namespace?.LastIndexOf('.') ?? -1;
                if (lastDotIndexInNamespace > -1)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    var directNamespace = type.Namespace.Substring(lastDotIndexInNamespace + 1);
                    typeDocUrl = $"{docFolder}{directNamespace}.{type.Name}.html";
                }
                else
                {
                    typeDocUrl = $"{docFolder}{type.Name}.html";
                }

                if (File.Exists(typeDocUrl))
                {
                    continue;
                }

                if (appendAssembly)
                {
                    appendAssembly = false;
                    var assemblyPath = assembly.Location.Remove(0, engineFolder.Length)
                        .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    result.AppendLine($"- {assemblyPath}");
                }

                result.AppendLine($"    - {type.FullName}");
            }
        }

        result.AppendLine();
        SaveResult(result);
    }

    private static void SaveResult(StringBuilder result, [CallerFilePath] string callerFilePath = "")
    {
        var folder = Path.GetDirectoryName(callerFilePath);
        Assert.IsNotNull(folder);
        var fileName = $"Unity {Application.unityVersion} Undocumented Types.md";
        var filePath = Path.Combine(folder, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        File.WriteAllText(filePath, result.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();

        var resultFilePath = Path.Combine(folder.Substring(Application.dataPath.Length - "Assets".Length), fileName);
        var resultFile = AssetDatabase.LoadAssetAtPath<TextAsset>(resultFilePath);
        Debug.Log($"Save undocumented types list to file {filePath}.", resultFile);
    }
}