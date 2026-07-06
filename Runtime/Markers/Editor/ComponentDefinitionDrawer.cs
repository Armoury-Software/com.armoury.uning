using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Armoury.UI.Markers.Elemental;

namespace Armoury.UI.Markers.Editor
{
    [CustomPropertyDrawer(typeof(DirectiveDefinition.UxmlSerializedData), useForChildren: false)]
    public sealed class DirectiveDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = UniNgDrawerUtils.DrawRootContainer();
            
            root.Add(UniNgDrawerUtils.DrawHeader(
                "DirectiveDefinition", 
                UniNgDrawerUtils.DrawUniNg(),
                UniNgDrawerUtils.DrawTypeBadge(property))
            );
            
            root.Add(new PropertyField(property.FindPropertyRelative(nameof(DirectiveDefinition.Inputs))));
            
            return root;
        }
    }
    
    [CustomPropertyDrawer(typeof(ComponentDefinition.UxmlSerializedData))]
    public sealed class ComponentDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = UniNgDrawerUtils.DrawRootContainer();
            
            MarkerFinderUtils.EnsureComponentDefinition(property, out var componentType);
            // EnsureInputsCollectionProperty(property, out _);
            
            var descriptors = ComponentInputRegistry.Get(componentType);
            
            root.Add(UniNgDrawerUtils.DrawHeader(
                componentType.Name,
                UniNgDrawerUtils.DrawUniNg(),
                UniNgDrawerUtils.DrawTypeBadge(property))
            );

            root.Add(DrawInputs());
            
            // var inputsProperty = property.FindPropertyRelative(nameof(DirectiveDefinition.Inputs));
            // var inputsCollectionProperty = inputsProperty.FindPropertyRelative(nameof(InputBindingsCollection.Inputs));
            
            return root;
        }

        private static VisualElement DrawInputs()
        {
            var container = UniNgDrawerUtils.DrawRootContainer();
            
            container.Add(UniNgDrawerUtils.DrawHeader(
                "Inputs",
                UniNgDrawerUtils.DrawBadge(24.ToString(), Color.cornflowerBlue)
            ));

            return container;
        }
    }
    
    internal static class UniNgDrawerUtils {
        internal static VisualElement DrawRootContainer()
        {
            var container = new VisualElement { name = "Container" };
            container.AddToClassList("uning-directive");

            AddSpacing(container);
            DrawBorders(container);

            return container;

            void AddSpacing(VisualElement target)
            {
                target.style.marginTop = 4;
                target.style.marginBottom = 8;
                
                target.style.paddingTop = 10;
                target.style.paddingRight = 10;
                target.style.paddingBottom = 10;
                target.style.paddingLeft = 10;
            }
            void DrawBorders(VisualElement target)
            {
                var borderColor = new Color(1, 1, 1f, 0.35f);
                
                target.style.borderTopColor = borderColor;
                target.style.borderRightColor = borderColor;
                target.style.borderBottomColor = borderColor;
                target.style.borderLeftColor = borderColor;

                target.style.borderTopWidth = 1;
                target.style.borderRightWidth = 1;
                target.style.borderBottomWidth = 1;
                target.style.borderLeftWidth = 1;

                target.style.borderTopLeftRadius = 4;
                target.style.borderTopRightRadius = 4;
                target.style.borderBottomRightRadius = 4;
                target.style.borderBottomLeftRadius = 4;
            }
        }
        
        internal static VisualElement DrawHeader(string title, params VisualElement[] badges)
        {
            var container = new VisualElement
            {
                name = "Header",
                style =
                {
                    alignItems = Align.Center,
                    flexDirection = FlexDirection.Row,
                    marginBottom = 8,
                }
            };
            
            container.AddToClassList("input-bindings-collection__header");
            
            foreach (var badge in badges)
            {
                container.Add(badge);
            }
            
            container.Add(new Label(title)
            {
                style =
                {
                    color = Color.whiteSmoke,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginLeft = badges.Length > 0 ? 6 : 0,
                    textOverflow = TextOverflow.Ellipsis,
                    overflow =  Overflow.Hidden,
                    flexShrink = 1,
                    flexGrow = 0,
                }
            });

            return container;
        }
        
        internal static VisualElement DrawBadge(string label, Color background, Color? color = null, bool addSpacing = false)
        {
            var badge = new Label(label)
            {
                name = "Badge: " + label,
                style =
                {
                    color = color ?? Color.gray1,
                    flexGrow = 0,
                    flexShrink = 0,
                    backgroundColor = background,
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    paddingTop = 2,
                    paddingRight = 6,
                    paddingBottom = 2,
                    paddingLeft = 6,
                    fontSize = 11,
                    marginLeft = addSpacing ? 6 : 0,
                }
            };

            return badge;
        }

        internal static VisualElement DrawTypeBadge(SerializedProperty property)
            => DrawTypeBadge(
                property.boxedValue is ComponentDefinition.UxmlSerializedData ? typeof(Component) : typeof(Directive)
            );

        internal static VisualElement DrawTypeBadge(Type type)
            => DrawBadge(
                type.Name,
                Color.lightPink,
                addSpacing: true
            );
        
        internal static VisualElement DrawUniNg()
        {
            return DrawBadge("UniNg", Color.mediumSpringGreen);
        }
    }
    
    internal static class MarkerFinderUtils
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static bool EnsureComponentDefinition(SerializedProperty property, out Type componentType)
        {
            componentType = null;
            
            if (property == null)
                return false;

            var serializedObject = property.serializedObject;

            serializedObject.Update();

            var markerSerializedDataProperty = FindOwningSerializedData(property);

            if (markerSerializedDataProperty == null)
            {
                Debug.LogWarning($"Could not find owning m_SerializedData for {property.propertyPath}");
                return false;
            }

            componentType = GetComponentTypeFromMarkerSerializedData(markerSerializedDataProperty);

            if (componentType == null)
            {
                Debug.LogWarning(
                    $"Could not infer TComp from marker serialized data: {markerSerializedDataProperty.propertyPath}"
                );

                return false;
            }

            var componentProperty = markerSerializedDataProperty.FindPropertyRelative("Component");

            if (componentProperty == null &&
                property.boxedValue is ComponentDefinition.UxmlSerializedData)
            {
                componentProperty = property;
            }

            if (componentProperty == null)
            {
                Debug.LogWarning("Could not find Component property.");
                return false;
            }

            Undo.RecordObject(serializedObject.targetObject, "Initialize Component Definition");

            var componentData =
                componentProperty.boxedValue as ComponentDefinition.UxmlSerializedData
                ?? new ComponentDefinition.UxmlSerializedData();

            var changed = SetGeneratedUxmlTypeField(
                componentData,
                "Type",
                componentType
            );

            if (!changed)
                return false;

            componentProperty.boxedValue = componentData;

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);

            return true;
        }

        private static SerializedProperty FindOwningSerializedData(SerializedProperty property)
        {
            const string marker = ".m_SerializedData";

            var path = property.propertyPath;
            var index = path.IndexOf(marker, StringComparison.Ordinal);

            if (index < 0)
                return null;

            var serializedDataPath = path.Substring(0, index + marker.Length);

            return property.serializedObject.FindProperty(serializedDataPath);
        }

        private static Type GetComponentTypeFromMarkerSerializedData(
            SerializedProperty markerSerializedDataProperty
        )
        {
            var markerSerializedData = markerSerializedDataProperty.boxedValue;

            if (markerSerializedData == null)
                return null;

            var serializedDataType = markerSerializedData.GetType();

            var markerType = serializedDataType.DeclaringType;

            if (markerType == null)
                return null;

            return GetComponentMarkerGenericArgument(markerType);
        }

        private static Type GetComponentMarkerGenericArgument(Type markerType)
        {
            for (var type = markerType; type != null; type = type.BaseType)
            {
                if (!type.IsGenericType)
                    continue;

                if (type.GetGenericTypeDefinition() != typeof(ComponentMarker<>))
                    continue;

                var componentType = type.GetGenericArguments()[0];

                if (componentType.ContainsGenericParameters)
                    return null;

                return componentType;
            }

            return null;
        }

        private static bool SetGeneratedUxmlTypeField(
            object uxmlSerializedData,
            string fieldName,
            Type componentType
        )
        {
            var field = FindField(uxmlSerializedData.GetType(), fieldName);

            if (field == null)
            {
                Debug.LogWarning(
                    $"Could not find generated UXML field '{fieldName}' on {uxmlSerializedData.GetType().FullName}"
                );

                return false;
            }

            object serializedValue;

            if (field.FieldType == typeof(Type))
            {
                serializedValue = componentType;
            }
            else if (field.FieldType == typeof(string))
            {
                serializedValue = $"{componentType.FullName}, {componentType.Assembly.GetName().Name}";
            }
            else
            {
                Debug.LogWarning(
                    $"Unsupported generated UXML Type backing field type: {field.FieldType.FullName}"
                );

                return false;
            }

            var oldValue = field.GetValue(uxmlSerializedData);

            if (Equals(oldValue, serializedValue))
                return false;

            field.SetValue(uxmlSerializedData, serializedValue);

            MarkUxmlAttributeAsOverridden(uxmlSerializedData, fieldName);

            return true;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                var field =
                    type.GetField(name, InstanceFlags) ??
                    type.GetField(char.ToLowerInvariant(name[0]) + name.Substring(1), InstanceFlags) ??
                    type.GetField($"m_{name}", InstanceFlags) ??
                    type.GetField($"m_{char.ToLowerInvariant(name[0])}{name.Substring(1)}", InstanceFlags);

                if (field != null)
                    return field;

                type = type.BaseType;
            }

            return null;
        }

        private static void MarkUxmlAttributeAsOverridden(
            object uxmlSerializedData,
            string attributeName
        )
        {
            for (var type = uxmlSerializedData.GetType(); type != null; type = type.BaseType)
            {
                foreach (var field in type.GetFields(InstanceFlags))
                {
                    if (!field.FieldType.IsEnum)
                        continue;

                    if (!field.FieldType.Name.Contains("UxmlAttributeFlags"))
                        continue;

                    if (field.Name.IndexOf(attributeName, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    object overriddenValue;

                    try
                    {
                        overriddenValue = Enum.Parse(field.FieldType, "OverriddenInUxml");
                    }
                    catch
                    {
                        overriddenValue = Enum.ToObject(field.FieldType, 1);
                    }

                    field.SetValue(uxmlSerializedData, overriddenValue);
                    return;
                }
            }
        }
    }
    
    /*[CustomPropertyDrawer(typeof(ComponentDefinition.UxmlSerializedData))]
    public sealed class ComponentDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var componentType = ComponentDefinitionSerializedUtility.GetComponentType(property);

            root.Add(new Label(componentType != null
                ? $"Component: {componentType.Name}"
                : "Component: <missing>"));

            if (componentType == null)
            {
                root.Add(new HelpBox(
                    "ComponentDefinition has no valid component type.",
                    HelpBoxMessageType.Error
                ));

                return root;
            }

            var descriptors = ComponentInputRegistry.Get(componentType);

            var inputsProperty = property.FindPropertyRelative("Inputs");
            var inputCountProperty = property.FindPropertyRelative("InputCount");

            var inputsContainer = new VisualElement();
            root.Add(inputsContainer);

            void Rebuild()
            {
                inputsContainer.Clear();

                DrawInputs(
                    inputsContainer,
                    property,
                    inputsProperty,
                    inputCountProperty,
                    descriptors
                );

                DrawAddInputButton(
                    inputsContainer,
                    property,
                    inputsProperty,
                    inputCountProperty,
                    descriptors,
                    Rebuild
                );
            }

            Rebuild();

            return root;
        }

        private static void DrawInputs(
            VisualElement root,
            SerializedProperty componentProperty,
            SerializedProperty inputsProperty,
            SerializedProperty inputCountProperty,
            InputDescriptor[] descriptors
        )
        {
            var count = inputCountProperty != null
                ? inputCountProperty.intValue
                : inputsProperty.arraySize;

            for (var i = 0; i < count; i++)
            {
                var bindingProperty = inputsProperty.GetArrayElementAtIndex(i);

                var box = new VisualElement();
                box.style.marginTop = 6;
                box.style.marginBottom = 6;
                box.style.paddingLeft = 6;
                box.style.paddingRight = 6;
                box.style.paddingTop = 6;
                box.style.paddingBottom = 6;

                DrawBinding(box, bindingProperty, descriptors);

                root.Add(box);
            }
        }

        private static void DrawBinding(
            VisualElement root,
            SerializedProperty bindingProperty,
            InputDescriptor[] descriptors
        )
        {
            var inputIdProperty = bindingProperty.FindPropertyRelative("InputId");
            var inputAliasProperty = bindingProperty.FindPropertyRelative("InputAlias");
            var sourceProperty = bindingProperty.FindPropertyRelative("Source");
            var kindProperty = bindingProperty.FindPropertyRelative("Kind");

            var inputId = inputIdProperty.ulongValue;

            var descriptor = FindDescriptor(descriptors, inputId);

            if (descriptor == null)
            {
                root.Add(new HelpBox(
                    $"Missing input: {inputAliasProperty.stringValue}",
                    HelpBoxMessageType.Error
                ));

                return;
            }

            root.Add(new Label($"{descriptor.Value.Alias} : {descriptor.Value.ValueType.Name}"));

            root.Add(new PropertyField(sourceProperty));

            var source = (ComponentInputValueSource)sourceProperty.enumValueIndex;

            if (source == ComponentInputValueSource.Literal)
            {
                DrawLiteralValue(root, bindingProperty, descriptor.Value);
            }
            else
            {
                DrawParentBinding(root, bindingProperty, descriptor.Value);
            }
        }

        private static void DrawLiteralValue(
            VisualElement root,
            SerializedProperty bindingProperty,
            InputDescriptor descriptor
        )
        {
            var literalProperty = bindingProperty.FindPropertyRelative("LiteralValue");

            if (literalProperty == null)
            {
                root.Add(new HelpBox(
                    "LiteralValue is missing.",
                    HelpBoxMessageType.Error
                ));

                return;
            }

            root.Add(new PropertyField(literalProperty));
        }

        private static void DrawParentBinding(
            VisualElement root,
            SerializedProperty bindingProperty,
            InputDescriptor inputDescriptor
        )
        {
            var bindingPathProperty = bindingProperty.FindPropertyRelative("BindingPath");
            var parentBindingIdProperty = bindingProperty.FindPropertyRelative("ParentBindingId");

            // Temporary simple version.
            // Later, replace this with a dropdown from ParentBindingRegistry.
            root.Add(new PropertyField(bindingPathProperty, "Parent Binding Path"));
            root.Add(new PropertyField(parentBindingIdProperty, "Parent Binding Id"));
        }

        private static void DrawAddInputButton(
            VisualElement root,
            SerializedProperty componentProperty,
            SerializedProperty inputsProperty,
            SerializedProperty inputCountProperty,
            InputDescriptor[] descriptors,
            System.Action rebuild
        )
        {
            var button = new Button(() =>
            {
                var menu = new GenericMenu();

                for (var i = 0; i < descriptors.Length; i++)
                {
                    var descriptor = descriptors[i];

                    if (HasInput(inputsProperty, inputCountProperty, descriptor.Id))
                    {
                        menu.AddDisabledItem(
                            new GUIContent($"{descriptor.Alias} ({descriptor.ValueType.Name})")
                        );

                        continue;
                    }

                    menu.AddItem(
                        new GUIContent($"{descriptor.Alias} ({descriptor.ValueType.Name})"),
                        false,
                        () =>
                        {
                            AddInput(
                                componentProperty,
                                inputsProperty,
                                inputCountProperty,
                                descriptor
                            );

                            componentProperty.serializedObject.ApplyModifiedProperties();
                            rebuild();
                        }
                    );
                }

                menu.ShowAsContext();
            })
            {
                text = "+ Add Input"
            };

            root.Add(button);
        }

        private static void AddInput(
            SerializedProperty componentProperty,
            SerializedProperty inputsProperty,
            SerializedProperty inputCountProperty,
            InputDescriptor descriptor
        )
        {
            var index = inputCountProperty.intValue;

            if (inputsProperty.arraySize <= index)
                inputsProperty.arraySize = index + 1;
            
            var bindingProperty = inputsProperty.GetArrayElementAtIndex(index);
            bindingProperty.PrintChildren();
            
            bindingProperty.FindPropertyRelative("InputId").ulongValue = descriptor.Id;
            bindingProperty.FindPropertyRelative("InputIndex").intValue = descriptor.Index;
            bindingProperty.FindPropertyRelative("InputName").stringValue = descriptor.MemberName;
            bindingProperty.FindPropertyRelative("InputAlias").stringValue = descriptor.Alias;
            bindingProperty.FindPropertyRelative("Kind").enumValueIndex = (int)descriptor.Kind;
            bindingProperty.FindPropertyRelative("Source").enumValueIndex =
                (int)ComponentInputValueSource.Literal;

            CreateDefaultLiteralValue(bindingProperty, descriptor);

            inputCountProperty.intValue = index + 1;
        }

        private static void CreateDefaultLiteralValue(
            SerializedProperty bindingProperty,
            InputDescriptor descriptor
        )
        {
            // This depends on how you implemented LiteralValue.
            //
            // If LiteralValue is a managed reference:
            // bindingProperty.FindPropertyRelative("LiteralValue").managedReferenceValue =
            //     InputValueDefinitionFactory.Create(descriptor);
            //
            // If LiteralValue is a UxmlObjectReference, you may need to create the
            // correct UxmlSerializedData object instead.
        }

        private static bool HasInput(
            SerializedProperty inputsProperty,
            SerializedProperty inputCountProperty,
            ulong inputId
        )
        {
            var count = inputCountProperty.intValue;

            for (var i = 0; i < count; i++)
            {
                var item = inputsProperty.GetArrayElementAtIndex(i);
                var itemInputId = item.FindPropertyRelative("InputId").ulongValue;

                if (itemInputId == inputId)
                    return true;
            }

            return false;
        }

        private static InputDescriptor? FindDescriptor(
            InputDescriptor[] descriptors,
            ulong inputId
        )
        {
            for (var i = 0; i < descriptors.Length; i++)
            {
                if (descriptors[i].Id == inputId)
                    return descriptors[i];
            }

            return null;
        }
    }
    
    internal static class ComponentDefinitionSerializedUtility
    {
        public static System.Type GetComponentType(SerializedProperty property)
        {
            var typeNameProperty =
                property.FindPropertyRelative("ComponentTypeName");

            if (typeNameProperty == null)
                return null;

            var typeName = typeNameProperty.stringValue;

            if (string.IsNullOrEmpty(typeName))
                return null;

            return System.Type.GetType(typeName);
        }
    }
    
    internal static class SerializedPropertyDebugExtensions
    {
        internal static void DebugPrintFindPropertyRelativePaths(
            this SerializedProperty root,
            bool visibleOnly = true
        )
        {
            if (root == null)
            {
                Debug.LogWarning("SerializedProperty is null.");
                return;
            }

            var iterator = root.Copy();
            var end = root.GetEndProperty();

            string rootPath = root.propertyPath;
            int rootDepth = root.depth;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Relative properties for:");
            sb.AppendLine($"  Full path: {rootPath}");
            sb.AppendLine();

            bool enterChildren = true;

            while (MoveNext(iterator, visibleOnly, enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                // Safety: only print actual children/grandchildren of root.
                if (iterator.depth <= rootDepth)
                    continue;

                string fullPath = iterator.propertyPath;
                string relativePath = MakeRelativePath(rootPath, fullPath);

                sb.Append(' ', (iterator.depth - rootDepth - 1) * 2);
                sb.Append("- ");
                sb.Append(relativePath);
                sb.Append("    ");
                sb.Append($"[{iterator.propertyType}]");
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        private static bool MoveNext(
            SerializedProperty property,
            bool visibleOnly,
            bool enterChildren
        )
        {
            return visibleOnly
                ? property.NextVisible(enterChildren)
                : property.Next(enterChildren);
        }

        private static string MakeRelativePath(string rootPath, string fullPath)
        {
            if (fullPath == rootPath)
                return string.Empty;

            string prefix = rootPath + ".";

            if (fullPath.StartsWith(prefix))
                return fullPath.Substring(prefix.Length);

            // Fallback. Should rarely happen unless Unity gives an unexpected path.
            return fullPath;
        }
        
        internal static void PrintChildren(this SerializedProperty property)
        {
            var iterator = property.Copy();
            var end = property.GetEndProperty();

            Debug.Log($"--- Children of {property.propertyPath} ---");

            var enterChildren = true;

            while (iterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(iterator, end))
            {
                enterChildren = false;

                if (iterator.depth <= property.depth)
                    continue;

                Debug.Log(iterator.propertyPath);
            }
        }
    }*/
}