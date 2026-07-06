#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Markers.Elemental.Editor
{
    internal static class ComponentTemplatePreview
    {
        public static void InstantiateInto<TComp>(VisualElement target)
            where TComp : Component
        {
            var componentType = typeof(TComp);

            var attribute = componentType.GetCustomAttribute<ComponentAttribute>(inherit: false);

            if (attribute == null)
            {
                AddError(
                    target,
                    $"{componentType.Name} is missing [{nameof(ComponentAttribute)}]."
                );
                return;
            }

            if (string.IsNullOrWhiteSpace(attribute.TemplatePath))
            {
                AddError(
                    target,
                    $"{componentType.Name}'s [{nameof(ComponentAttribute)}] has no templatePath."
                );
                return;
            }

            if (!TryResolveTemplateAssetPath(
                    componentType,
                    attribute.TemplatePath,
                    out var assetPath,
                    out var error
                ))
            {
                AddError(target, error);
                return;
            }

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);

            if (visualTree == null)
            {
                AddError(
                    target,
                    $"Could not load VisualTreeAsset at path: {assetPath}"
                );
                return;
            }

            VisualElement instance = visualTree.CloneTree();
            target.Add(instance);
        }

        private static bool TryResolveTemplateAssetPath(
            Type componentType,
            string templatePath,
            out string assetPath,
            out string error
        )
        {
            assetPath = null;
            error = null;

            templatePath = StripProjectDatabasePrefix(templatePath.Trim());

            if (IsProjectRelativeAssetPath(templatePath))
            {
                assetPath = NormalizeAssetPath(templatePath);
                return true;
            }

            if (!TryFindScriptPath(componentType, out string scriptPath))
            {
                error =
                    $"Could not find the MonoScript asset for component type {componentType.FullName}. " +
                    $"Cannot resolve relative template path: {templatePath}";
                return false;
            }

            var scriptDirectory = Path
                .GetDirectoryName(scriptPath)
                ?.Replace('\\', '/');

            if (string.IsNullOrEmpty(scriptDirectory))
            {
                error = $"Could not resolve script directory for {componentType.FullName}.";
                return false;
            }

            assetPath = NormalizeAssetPath($"{scriptDirectory}/{templatePath}");
            return true;
        }

        private static bool TryFindScriptPath(Type type, out string scriptPath)
        {
            scriptPath = null;

            var guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);

                if (script == null)
                {
                    continue;
                }

                if (script.GetClass() == type)
                {
                    scriptPath = path;
                    return true;
                }
            }

            return false;
        }

        private static bool IsProjectRelativeAssetPath(string path)
        {
            return path.StartsWith("Assets/", StringComparison.Ordinal) ||
                   path.StartsWith("Packages/", StringComparison.Ordinal);
        }

        private static string StripProjectDatabasePrefix(string path)
        {
            const string prefix = "project://database/";

            if (!path.StartsWith(prefix, StringComparison.Ordinal))
            {
                return path;
            }

            path = path.Substring(prefix.Length);

            var queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
            {
                path = path.Substring(0, queryIndex);
            }

            var hashIndex = path.IndexOf('#');
            if (hashIndex >= 0)
            {
                path = path.Substring(0, hashIndex);
            }

            return Uri.UnescapeDataString(path);
        }

        private static string NormalizeAssetPath(string path)
        {
            path = path.Replace('\\', '/');

            var rawParts = path.Split('/');
            List<string> parts = new(rawParts.Length);

            foreach (var rawPart in rawParts)
            {
                if (string.IsNullOrEmpty(rawPart) || rawPart == ".")
                {
                    continue;
                }

                if (rawPart == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }

                    continue;
                }

                parts.Add(rawPart);
            }

            if (parts.Contains("_UniNg"))
            {
                parts.Remove("_UniNg");
            }

            return string.Join("/", parts);
        }

        private static void AddError(VisualElement target, string message)
        {
            target.Add(new HelpBox(message, HelpBoxMessageType.Error));
            Debug.LogError(message);
        }
    }
}
#endif
