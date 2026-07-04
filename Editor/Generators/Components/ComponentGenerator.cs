using Armoury.UI.Injectors;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Editor.Generators
{
    public static class ComponentGenerator
    {
        private const string PackageName = "com.armoury.uning";
        private const string ComponentIconPath = "Packages/" + PackageName + "/Editor/Icons/component.png";
        private const string DefaultDir = "Assets", DefaultName = "NgComponent";
        private const string PendingKey = "Armoury.UI.ComponentGenerator.Pending";
        
        [MenuItem("Assets/Create/UniNg/Component", false, 80)]
        private static void CreateComponent()
        {
            var folder = GetSelectedProjectFolder();

            if (!IsCleanFolder(folder))
            {
                ShowFolderNotCleanDialog(folder);
                return;
            }

            var path = AssetDatabase.GenerateUniqueAssetPath(folder + $"/{DefaultName}.cs");

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                0,
                ScriptableObject.CreateInstance<ComponentGeneratorAction>(),
                path,
                AssetDatabase.LoadAssetAtPath<Texture2D>(ComponentIconPath),
                null
            );
        }
        
        private static string GetSelectedProjectFolder()
        {
            var selected = Selection.activeObject;

            if (selected == null)
                return DefaultDir;

            var path = AssetDatabase.GetAssetPath(selected);

            if (string.IsNullOrEmpty(path))
                return DefaultDir;

            if (System.IO.Directory.Exists(path))
                return path.Replace("\\", "/");

            var directory = System.IO.Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(directory))
                return DefaultDir;

            return directory.Replace("\\", "/");
        }

        private static bool IsCleanFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return false;

            if (!System.IO.Directory.Exists(folder))
                return false;

            var files = System.IO.Directory.GetFiles(folder);

            for (var i = 0; i < files.Length; i++)
            {
                var extension = System.IO.Path.GetExtension(files[i]);

                if (!string.Equals(extension, ".meta", System.StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static void ShowFolderNotCleanDialog(string folder)
        {
            EditorUtility.DisplayDialog(
                "Cannot Create UniNg Component",
                $"The selected folder cannot contain regular files before creating a UniNg component.\n\nFolder:\n{folder}\n\nOnly .meta files and directories are allowed.",
                "OK"
            );
        }
        
        internal static void CreateFiles(string requestedPath)
        {
            var folder = System.IO.Path.GetDirectoryName(requestedPath);

            if (string.IsNullOrEmpty(folder))
                folder = DefaultDir;

            folder = folder.Replace("\\", "/");

            if (!IsCleanFolder(folder))
            {
                ShowFolderNotCleanDialog(folder);
                return;
            }

            var requestedName = System.IO.Path.GetFileNameWithoutExtension(requestedPath);
            var componentName = ToValidTypeName(requestedName);
            var providersName = componentName + "Providers";

            var componentPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + componentName + ".cs"
            );

            var uxmlPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + componentName + ".uxml"
            );

            var ussPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + componentName + ".uss"
            );

            var assetPath = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + providersName + ".asset"
            );

            CreateTextAsset(ussPath, GenerateUssCode(componentName));
            CreateTextAsset(uxmlPath, GenerateUxmlCode(componentName));
            CreateTextAsset(componentPath, GenerateComponentCode(componentName));

            var pending = new PendingComponentAsset
            {
                ComponentName = componentName,
                ProvidersName = providersName,
                ProvidersTypeName = componentName + "+Provider",
                AssetPath = assetPath,
                UxmlPath = uxmlPath
            };

            SessionState.SetString(PendingKey, JsonUtility.ToJson(pending));

            AssetDatabase.ImportAsset(componentPath);
            AssetDatabase.ImportAsset(uxmlPath);
            AssetDatabase.ImportAsset(ussPath);
            AssetDatabase.Refresh();
        }
        
        private static string GenerateComponentCode(string componentName)
        {
            var body =
                $@"using Armoury.UI;
using Armoury.UI.Injectors;

public sealed class {componentName} : Component
{{
    public {componentName}() : this(null) {{ }}
    public {componentName}(Injector injector) : base(injector) {{ }}

    protected override void OnInjected() {{ }}

    // ReSharper disable once UnusedType.Global
    public class Provider : ComponentMetadata<{componentName}>.Provider {{ }}
}}
";

            return body;
        }
        
        private static string GenerateUxmlCode(string componentName)
        {
            return $@"<ui:UXML xmlns:ui=""UnityEngine.UIElements"">
    <Style src=""{componentName}.uss"" />
    <ui:Label text=""{componentName} works!""/>
</ui:UXML>
";
        }

        private static string GenerateUssCode(string componentName) => ":root {}";
        
        private static void CreateTextAsset(string path, string content)
        {
            ProjectWindowUtil.CreateScriptAssetWithContent(path, content);
        }
        
        private static string ToValidTypeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return DefaultName;

            var builder = new System.Text.StringBuilder(value.Length);
            var capitalizeNext = true;

            for (var i = 0; i < value.Length; i++)
            {
                var c = value[i];

                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    if (builder.Length == 0 && char.IsDigit(c))
                        builder.Append('_');

                    if (capitalizeNext && char.IsLetter(c))
                        c = char.ToUpperInvariant(c);

                    builder.Append(c);
                    capitalizeNext = false;
                }
                else
                {
                    capitalizeNext = true;
                }
            }

            if (builder.Length == 0)
                return DefaultName;

            return builder.ToString();
        }
        
        [DidReloadScripts]
        private static void CreatePendingAssetAfterReload()
        {
            var json = SessionState.GetString(PendingKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return;

            var pending = JsonUtility.FromJson<PendingComponentAsset>(json);

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(pending.AssetPath) != null)
            {
                SessionState.EraseString(PendingKey);
                return;
            }

            var providerType = FindType(pending.ProvidersTypeName);

            if (providerType == null)
            {
                Debug.LogWarning(
                    $"Could not find generated provider type '{pending.ProvidersTypeName}' yet."
                );

                return;
            }

            if (!typeof(Provider).IsAssignableFrom(providerType))
            {
                SessionState.EraseString(PendingKey);

                Debug.LogError(
                    $"Generated provider type '{pending.ProvidersTypeName}' does not inherit Provider."
                );

                return;
            }

            AssetDatabase.ImportAsset(pending.UxmlPath, ImportAssetOptions.ForceSynchronousImport);

            var uxmlAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                pending.UxmlPath
            );

            if (uxmlAsset == null)
            {
                SessionState.EraseString(PendingKey);

                Debug.LogError(
                    $"Could not load generated UXML asset at '{pending.UxmlPath}'."
                );

                return;
            }

            var provider = (Provider)
                System.Activator.CreateInstance(providerType);

            var componentType = providerType.DeclaringType;

            if (componentType == null)
            {
                SessionState.EraseString(PendingKey);

                Debug.LogError(
                    $"Could not find declaring component type for generated provider type '{pending.ProvidersTypeName}'."
                );

                return;
            }

            var asset = ScriptableObject.CreateInstance<EnvironmentInjectorDefinition>();
            asset.name = pending.ProvidersName;

            if (asset.Providers == null)
                asset.Providers = new System.Collections.Generic.List<IProviderCollection>();

            asset.Providers.Add(provider);
            
            var metadataType = typeof(ComponentMetadata<>).MakeGenericType(
                componentType
            );

            var metadata = System.Activator.CreateInstance(
                metadataType,
                new object[] { uxmlAsset }
            );

            if (!TrySetComponentMetadataUxml(metadata, uxmlAsset))
            {
                SessionState.EraseString(PendingKey);

                Debug.LogError(
                    $"Could not assign UXML for generated provider type '{pending.ProvidersTypeName}'."
                );

                return;
            }

            if (!TrySetValueProviderValue(provider, metadata))
            {
                SessionState.EraseString(PendingKey);

                Debug.LogError(
                    $"Could not assign Value for generated provider type '{pending.ProvidersTypeName}'."
                );

                return;
            }

            AssetDatabase.CreateAsset(asset, pending.AssetPath);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            ProjectWindowUtil.ShowCreatedAsset(asset);

            SessionState.EraseString(PendingKey);
        }
        
        private static bool TrySetComponentMetadataUxml(object metadata, VisualTreeAsset uxmlAsset)
        {
            if (metadata == null || uxmlAsset == null)
                return false;

            var field = FindField(metadata.GetType(), "_uxml");

            if (field == null)
                return false;

            if (!typeof(VisualTreeAsset).IsAssignableFrom(field.FieldType))
                return false;

            field.SetValue(metadata, uxmlAsset);

            return field.GetValue(metadata) == uxmlAsset;
        }

        private static bool TrySetValueProviderValue(
            Provider provider,
            object value
        )
        {
            var type = provider.GetType();

            while (type != null)
            {
                const System.Reflection.BindingFlags flags =
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly;

                var property = type.GetProperty("Value", flags);

                if (property != null && property.PropertyType.IsInstanceOfType(value))
                {
                    var setter = property.GetSetMethod(true);

                    if (setter != null)
                    {
                        setter.Invoke(provider, new[] { value });
                        return true;
                    }
                }

                var field =
                    type.GetField("Value", flags) ??
                    type.GetField("_value", flags) ??
                    type.GetField("<Value>k__BackingField", flags);

                if (field != null && field.FieldType.IsInstanceOfType(value))
                {
                    field.SetValue(provider, value);
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static System.Reflection.FieldInfo FindField(System.Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(
                    name,
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly
                );

                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private static System.Type FindType(string fullName)
        {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            for (var i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(fullName);

                if (type != null)
                    return type;
            }

            return null;
        }
        
        [System.Serializable]
        private sealed class PendingComponentAsset
        {
            public string ComponentName;
            public string ProvidersName;
            public string ProvidersTypeName;
            public string AssetPath;
            public string UxmlPath;
        }
    }
    
    public sealed class ComponentGeneratorAction : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            ComponentGenerator.CreateFiles(pathName);
        }
    }
}
