using System;
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
            var componentType = MarkerFinderUtils.FindComponentTypeFromProperty(property);
            
            var root = UniNgDrawerUtils.DrawRootContainer();
            
            root.Add(UniNgDrawerUtils.DrawHeader(
                componentType.Name,
                UniNgDrawerUtils.DrawUniNg(),
                UniNgDrawerUtils.DrawTypeBadge(property))
            );

            var inputsProperty = property.FindPropertyRelative(nameof(DirectiveDefinition.Inputs));
            
            root.Add(DrawInputs(componentType, property, inputsProperty));
            
            return root;
        }

        private static VisualElement DrawInputs(
            Type componentType,
            SerializedProperty definitionProperty,
            SerializedProperty inputsProperty
        )
        {
            var descriptors = ComponentInputRegistry.Get(componentType);
            
            var container = UniNgDrawerUtils.DrawRootContainer();
            
            container.Add(UniNgDrawerUtils.DrawHeader(
                "Inputs",
                UniNgDrawerUtils.DrawBadge(descriptors.Length.ToString(), Color.cornflowerBlue)
            ));

            for (var i = 0; i < descriptors.Length; i++)
            {
                var bindingsArrayProperty =
                    definitionProperty.FindPropertyRelative(nameof(DirectiveDefinition.Inputs));
                
                if (inputsProperty.arraySize <= i)
                {
                    AddInput(
                        definitionProperty,
                        bindingsArrayProperty,
                        descriptors[i]
                    );
                }
                
                bindingsArrayProperty =
                    definitionProperty.FindPropertyRelative(nameof(DirectiveDefinition.Inputs));
                
                container.Add(DrawSingleInput(
                    in descriptors[i],
                    definitionProperty,
                    bindingsArrayProperty,
                    inputsProperty.GetArrayElementAtIndex(i)
                ));
            }

            return container;
        }

        private static VisualElement DrawSingleInput(
            in InputDescriptor descriptor,
            SerializedProperty definitionProperty,
            SerializedProperty inputsProperty,
            SerializedProperty inputProperty
        )
        {
            var container = new VisualElement
            {
                name = $"{descriptor.DisplayName} Input",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    marginTop = 4,
                    marginBottom = 4,
                }
            };
            
            container.Add(new Toggle { style = { marginRight = 6, marginBottom = 0 } });
            
            var innerContainer = new VisualElement
            {
                name = "Inner Container",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    alignItems = Align.Center,
                    // opacity = 0.5f
                }
            };
            container.Add(innerContainer);

            var inputSource = (InputValueSource) inputProperty.FindPropertyRelative(nameof(InputBinding.Source)).enumValueIndex;

            var valueField = inputSource == InputValueSource.Literal
                ? DrawLiteralField(definitionProperty, inputProperty)
                : DrawBindingField(definitionProperty, inputProperty);
            
            innerContainer.Add(
                descriptor.Kind switch
                {
                    InputValueKind.None => new Label("None"),
                    _ => valueField
                }
            );

            return container;
        }

        private static VisualElement DrawLiteralField(
            SerializedProperty definitionProperty,
            SerializedProperty inputProperty
        )
        {
            var literalValueProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.LiteralValue));
            var inputNameValue = inputProperty.FindPropertyRelative("InputName").stringValue;
            var actualValueProperty = literalValueProperty.FindPropertyRelative("Value");
            var valueField = new PropertyField(actualValueProperty) { label = inputNameValue, style = { flexGrow = 1 } };
            
            valueField.RegisterCallback<SerializedPropertyChangeEvent>(_ =>
            {
                var serializedObject = definitionProperty.serializedObject;
                var definitionPath = definitionProperty.propertyPath;
                var inputPath = inputProperty.propertyPath;

                serializedObject.Update();

                var freshDefinition = serializedObject.FindProperty(definitionPath);
                var freshInput = serializedObject.FindProperty(inputPath);

                var freshLiteral =
                    freshInput.FindPropertyRelative(nameof(InputBinding.LiteralValue));

                MarkUxmlAttributeAsOverridden(
                    freshDefinition,
                    nameof(DirectiveDefinition.Inputs)
                );

                MarkUxmlAttributeAsOverridden(
                    freshInput,
                    nameof(InputBinding.LiteralValue)
                );

                MarkUxmlAttributeAsOverridden(
                    freshLiteral,
                    "Value"
                );

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
            });

            return valueField;
        }
        
        private static VisualElement DrawBindingField(
            SerializedProperty definitionProperty,
            SerializedProperty inputProperty
        )
        {
            var root = new VisualElement();
            
            var bindingPathProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.BindingPath));
            var parentBindingIdProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.ParentBindingId));
            
            // TODO: Add actual binding parent picker
            root.Add(new PropertyField(bindingPathProperty, "Parent Binding Path"));
            root.Add(new PropertyField(parentBindingIdProperty, "Parent Binding Id"));

            return root;
        }
        
        private static void AddInput(
            SerializedProperty definitionProperty,
            SerializedProperty bindingsArrayProperty,
            InputDescriptor descriptor
        )
        {
            var serializedObject = definitionProperty.serializedObject;

            var definitionPath = definitionProperty.propertyPath;
            var arrayPath = bindingsArrayProperty.propertyPath;

            Undo.RecordObject(serializedObject.targetObject, "Add Component Input");

            serializedObject.Update();

            definitionProperty = serializedObject.FindProperty(definitionPath);
            bindingsArrayProperty = serializedObject.FindProperty(arrayPath);

            MarkUxmlAttributeAsOverridden(
                definitionProperty,
                nameof(DirectiveDefinition.Inputs)
            );

            var index = bindingsArrayProperty.arraySize;
            bindingsArrayProperty.arraySize++;

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            bindingsArrayProperty = serializedObject.FindProperty(arrayPath);
            var bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(index);

            bindingProperty.managedReferenceValue =
                UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(InputBinding));

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            bindingsArrayProperty = serializedObject.FindProperty(arrayPath);
            bindingProperty = bindingsArrayProperty.GetArrayElementAtIndex(index);

            SetWithOverride(bindingProperty, nameof(InputBinding.InputId), p => p.ulongValue = descriptor.Id);
            SetWithOverride(bindingProperty, nameof(InputBinding.InputIndex), p => p.intValue = descriptor.Index);
            SetWithOverride(bindingProperty, nameof(InputBinding.InputName), p => p.stringValue = descriptor.MemberName);
            SetWithOverride(bindingProperty, nameof(InputBinding.InputAlias), p => p.stringValue = descriptor.Alias);
            SetWithOverride(bindingProperty, nameof(InputBinding.Kind), p => p.enumValueIndex = (int)descriptor.Kind);
            SetWithOverride(bindingProperty, nameof(InputBinding.Source), p => p.enumValueIndex = (int)InputValueSource.Literal);

            var literalValueProperty = bindingProperty.FindPropertyRelative(nameof(InputBinding.LiteralValue));

            literalValueProperty.managedReferenceValue =
                UxmlSerializedDataCreator.CreateUxmlSerializedData(
                    descriptor.Kind switch
                    {
                        InputValueKind.Bool => typeof(BoolInputValueDefinition),
                        InputValueKind.Int => typeof(IntInputValueDefinition),
                        InputValueKind.Float => typeof(FloatInputValueDefinition),
                        InputValueKind.Double => typeof(DoubleInputValueDefinition),
                        InputValueKind.String => typeof(StringInputValueDefinition),
                        InputValueKind.Object => typeof(ObjectInputValueDefinition),
                        _ => throw new Exception($"Unhandled descriptor.Kind {descriptor.Kind}")
                    });

            MarkUxmlAttributeAsOverridden(bindingProperty, nameof(InputBinding.LiteralValue));

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(serializedObject.targetObject);
        }

        private static void SetWithOverride(
            SerializedProperty owner,
            string propertyName,
            Action<SerializedProperty> assign
        )
        {
            var child = owner.FindPropertyRelative(propertyName);
            if (child == null)
            {
                Debug.LogWarning($"Could not find property '{propertyName}' under {owner.propertyPath}");
                return;
            }

            assign(child);
            MarkUxmlAttributeAsOverridden(owner, propertyName);
        }
        
        private static bool HasInput(
            SerializedProperty inputsProperty,
            ulong inputId
        )
        {
            var count = inputsProperty.arraySize;
            
            if ((int) inputId >= count)
                return false;

            for (var i = 0; i < count; i++)
            {
                var item = inputsProperty.GetArrayElementAtIndex(i);
                var itemInputId = item.FindPropertyRelative("InputId").ulongValue;

                if (itemInputId == inputId)
                    return true;
            }

            return false;
        }
        
        private static void MarkUxmlAttributeAsOverridden(
            SerializedProperty uxmlSerializedDataProperty,
            string attributeName
        )
        {
            var flagsProperty =
                uxmlSerializedDataProperty.FindPropertyRelative($"{attributeName}_UxmlAttributeFlags") ??
                uxmlSerializedDataProperty.FindPropertyRelative($"{attributeName}UxmlAttributeFlags") ??
                uxmlSerializedDataProperty.FindPropertyRelative($"m_{attributeName}_UxmlAttributeFlags") ??
                uxmlSerializedDataProperty.FindPropertyRelative($"m_{attributeName}UxmlAttributeFlags");

            if (flagsProperty == null)
            {
                Debug.LogWarning(
                    $"Could not find UXML flags for '{attributeName}' under {uxmlSerializedDataProperty.propertyPath}"
                );

                return;
            }

            flagsProperty.intValue =
                (int)UxmlSerializedData.UxmlAttributeFlags.OverriddenInUxml;
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
        internal static Type FindComponentTypeFromProperty(SerializedProperty property)
        {
            if (property == null)
                return null;

            var serializedObject = property.serializedObject;

            serializedObject.Update();

            var markerSerializedDataProperty = FindOwningSerializedData(property);

            if (markerSerializedDataProperty == null)
            {
                Debug.LogWarning($"Could not find owning m_SerializedData for {property.propertyPath}");
                return null;
            }

            return GetComponentTypeFromMarkerSerializedData(markerSerializedDataProperty);
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
    }
}