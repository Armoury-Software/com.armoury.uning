using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

using Armoury.UI.Markers.Elemental;
using UnityEditor.UIElements;

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
        private const string InputBindingSourceImageClass = "input-bindings-collection__item__binding-image";
        
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
                    i,
                    componentType,
                    in descriptors[i],
                    definitionProperty,
                    bindingsArrayProperty,
                    inputsProperty.GetArrayElementAtIndex(i)
                ));
            }

            return container;
        }

        private static VisualElement DrawSingleInput(
            int index,
            Type componentType,
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

            var enabledToggle = new Toggle { style = { marginRight = 6, marginBottom = 0 } };
            enabledToggle.BindProperty(inputProperty.FindPropertyRelative(nameof(InputBinding.Enabled)));
            container.Add(enabledToggle);
            
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

            ToggleEnabled(enabledToggle.value, innerContainer);
            enabledToggle.RegisterValueChangedCallback(evt => ToggleEnabled(evt.newValue, innerContainer));
            
            container.Add(innerContainer);

            var inputSourceProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.Source));
            var inputSource = (InputValueSource) inputSourceProperty.enumValueIndex;

            var valueField = inputSource == InputValueSource.ParentBinding
                ? DrawBindingField(
                    descriptor.Kind,
                    descriptor.ValueType,
                    definitionProperty, 
                    inputProperty, 
                    () => RebuildSingleInput(
                        container, index, componentType, definitionProperty, inputsProperty, inputProperty
                    )
                )
                : DrawLiteralField(descriptor.Kind, definitionProperty, inputProperty);
            
            innerContainer.Add(
                descriptor.Kind switch
                {
                    InputValueKind.None => new Label("None"),
                    _ => valueField
                }
            );

            var toggle = DrawSourceButton(out var bindingImage);
            innerContainer.Add(toggle);
            
            SetSourceButtonState(bindingImage, inputSource == InputValueSource.ParentBinding);
            toggle.RegisterValueChangedCallback(evt =>
            {
                SetSourceButtonState(bindingImage, evt.newValue);
                OnInputValueSourceChanged(inputProperty, bindingImage, evt);
                RebuildSingleInput(container, index, componentType, definitionProperty, inputsProperty, inputProperty);
            });

            return container;

            Toggle DrawSourceButton(out Image image)
            {
                var toggleElement = new Toggle
                {
                    name = "Input Binding Source", 
                    style = { height = 18, width = 18, marginTop = 3 }
                };
                
                var checkmark = toggleElement.Q<VisualElement>(name: "unity-checkmark");
                checkmark.style.width = checkmark.style.height = new Length(100, LengthUnit.Percent);

                image = new Image
                {
                    image = EditorGUIUtility.IconContent("UnLinked").image as Texture2D,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore,
                    style =
                    {
                        backgroundColor = new Color(0.16f, 0.16f, 0.16f),
                        width = 16,
                        height = 16,
                        position = Position.Absolute,
                        left = 1,
                        top = 1,
                        borderTopRightRadius = 2,
                        borderBottomRightRadius = 2,
                        borderBottomLeftRadius = 2,
                        borderTopLeftRadius = 2
                    }
                };
                image.AddToClassList(InputBindingSourceImageClass);
                
                toggleElement.Add(image);

                return toggleElement;
            }
            void SetSourceButtonState(Image image, bool isBound)
            {
                image.style.backgroundColor = isBound
                    ? new Color(0.41f, 0.41f, 0.41f)
                    : new Color(0.16f, 0.16f, 0.16f);

                image.image = EditorGUIUtility.IconContent(
                    isBound ? "Linked" : "UnLinked"
                ).image as Texture2D;
            }

            static void ToggleEnabled(bool newValue, VisualElement container)
            {
                container.style.opacity = newValue ? 1.0f : 0.5f;
            }
        }

        private static void RebuildSingleInput(
            VisualElement input,
            int index,
            Type componentType,
            SerializedProperty definitionProperty,
            SerializedProperty inputsProperty,
            SerializedProperty inputProperty
        )
        {
            var descriptor = ComponentInputRegistry.Get(componentType)[index];
            var parent = input.parent;
            var indexInHierarchy = parent.IndexOf(input);
            
            input.RemoveFromHierarchy();

            parent.Insert(
                indexInHierarchy,
                DrawSingleInput(
                    index,
                    componentType,
                    in descriptor,
                    definitionProperty,
                    inputsProperty,
                    inputProperty
                ));
        }

        private static void OnInputValueSourceChanged(
            SerializedProperty inputProperty,
            Image image,
            ChangeEvent<bool> evt
        )
        {
            var sourceProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.Source));
            var newValue = Mathf.FloorToInt(Mathf.Repeat(sourceProperty.enumValueIndex + 1,
                Enum.GetValues(typeof(InputValueSource)).Length - 0.1f));
            
            sourceProperty.enumValueIndex = newValue;
            sourceProperty.serializedObject.Update();
            sourceProperty.serializedObject.ApplyModifiedProperties();
            
            SetWithOverride(inputProperty, nameof(InputBinding.Source), p => p.enumValueIndex = newValue);
            MarkUxmlAttributeAsOverridden(inputProperty, nameof(InputBinding.Source));

            inputProperty.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(inputProperty.serializedObject.targetObject);
        }

        private static VisualElement DrawLiteralField(
            InputValueKind kind,
            SerializedProperty definitionProperty,
            SerializedProperty inputProperty
        )
        {
            var inputNameValue = inputProperty.FindPropertyRelative(nameof(InputBinding.InputName)).stringValue;
            var literalValueProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.LiteralValue));
            var actualValueProperty = literalValueProperty.FindPropertyRelative(nameof(ObjectInputValueDefinition.Value));

            if (kind == InputValueKind.Value)
                return new Label(inputNameValue) { style = { flexGrow = 1 } };
            
            var valueField = new PropertyField(actualValueProperty) { label = inputNameValue, style = { flexGrow = 1 } };
            valueField.BindProperty(actualValueProperty);

            return valueField;
        }
        
        private static VisualElement DrawBindingField(
            InputValueKind kind,
            Type expectedValueType,
            SerializedProperty definitionProperty,
            SerializedProperty inputProperty,
            Action changed
        )
        {
            var inputNameValue = inputProperty.FindPropertyRelative(nameof(InputBinding.InputName)).stringValue;
            var bindingPathProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.BindingPath));
            var collectionIndexProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.CollectionIndex));
            var parentBindingIdProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.ParentBindingId));
            var inputSourceProperty = inputProperty.FindPropertyRelative(nameof(InputBinding.Source));

            var root = new VisualElement
            {
                name = "Bound Input Field",
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexGrow = 1,
                }
            };

            root.Add(new Label(inputNameValue + ":")
            {
                style =
                {
                    marginRight = 16
                }
            });

            var collectionIndex = collectionIndexProperty.intValue;

            var displayedPath = collectionIndex switch
            {
                InputBinding.CollectionDynamic =>
                    $"{bindingPathProperty.stringValue}[dynamic]",

                >= 0 =>
                    $"{bindingPathProperty.stringValue}[{collectionIndex}]",

                _ =>
                    bindingPathProperty.stringValue
            };

            var bindingValue = (
                !string.IsNullOrEmpty(bindingPathProperty.stringValue) &&
                parentBindingIdProperty.ulongValue != 0
            )
                ? $"{displayedPath} / {parentBindingIdProperty.ulongValue}"
                : "Unset";
            
            root.Add(new Label(bindingValue)
            {
                style =
                {
                    textOverflow = TextOverflow.Ellipsis,
                    overflow = Overflow.Hidden,
                    flexShrink = 1,
                    flexGrow = 1,
                    unityFontStyleAndWeight = FontStyle.Normal,
                    opacity = 0.7f
                }
            });

            var editButton = new Button()
            {
                text = "",
                iconImage = EditorGUIUtility.IconContent("editicon.sml").image as Texture2D,
            };
            root.Add(editButton);
            
            var descriptorKind = kind;
            var parentTypeProperty = definitionProperty.FindPropertyRelative(nameof(DirectiveDefinition.ParentType));
            var parentType = SerializedTypeUtility.GetTypeValue(parentTypeProperty);
            
            editButton.RegisterCallback<ClickEvent>(_ =>
            {
                ParentBindingPathWindow.Open(
                    dataSourceType: parentType,
                    expectedValueType: expectedValueType,
                    expectedKind: descriptorKind,
                    onDataSourceTypeSelected: selectedType =>
                    {
                        parentTypeProperty.serializedObject.Update();

                        var property = parentTypeProperty.serializedObject.FindProperty(parentTypeProperty.propertyPath);

                        SerializedTypeUtility.SetTypeValue(property, selectedType);

                        parentTypeProperty.serializedObject.ApplyModifiedProperties();
                        parentTypeProperty.serializedObject.Update();
                    },
                    onBindingSelected: (selectedType, selection) =>
                    {
                        inputProperty.serializedObject.Update();

                        inputSourceProperty.enumValueIndex = (int)InputValueSource.ParentBinding;
                    
                        parentBindingIdProperty.ulongValue = selection.Descriptor.Id;
                        bindingPathProperty.stringValue = selection.Descriptor.Path;
                        collectionIndexProperty.intValue = selection.CollectionIndex ?? InputBinding.CollectionUnspecified;

                        inputProperty.serializedObject.ApplyModifiedProperties();
                        inputProperty.serializedObject.Update();

                        changed();
                    }); 
            });

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
            SetWithOverride(bindingProperty, nameof(InputBinding.CollectionIndex), p => p.intValue = InputBinding.CollectionUnspecified);
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
                        InputValueKind.Value => typeof(ValueInputValueDefinition),
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
            
            container.Add(new Label(title)
            {
                style =
                {
                    color = Color.whiteSmoke,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginRight = 6,
                    textOverflow = TextOverflow.Ellipsis,
                    overflow =  Overflow.Hidden,
                    flexShrink = 1,
                    flexGrow = 0,
                }
            });
            
            container.AddToClassList("input-bindings-collection__header");
            
            foreach (var badge in badges)
            {
                container.Add(badge);
            }

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

        private static VisualElement DrawTypeBadge(Type type)
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
    
    public sealed class ParentBindingPathWindow : EditorWindow
    {
        private Type _dataSourceType;
        private InputValueKind? _expectedKind;
        private Type _expectedValueType;

        private Action<Type> _onDataSourceTypeSelected;
        private Action<Type, ParentBindingSelection> _onBindingSelected;

        private readonly List<TypeChoice> _typeChoices = new();
        private readonly List<Entry> _allEntries = new();
        private readonly List<Entry> _filteredEntries = new();
        
        private VisualElement _collectionIndexContainer;
        private IntegerField _collectionIndexField;
        private Toggle _dynamicCollectionIndexToggle;

        private PopupField<TypeChoice> _typeField;
        private TextField _searchField;
        private ListView _listView;
        private HelpBox _helpBox;
        private Button _bindButton;

        public static void Open(
            Type dataSourceType,
            InputValueKind? expectedKind,
            Type expectedValueType,
            Action<Type> onDataSourceTypeSelected,
            Action<Type, ParentBindingSelection> onBindingSelected)
        {
            var window = CreateInstance<ParentBindingPathWindow>();

            window._dataSourceType = dataSourceType;
            window._expectedKind = expectedKind;
            window._expectedValueType = expectedValueType;
            window._onDataSourceTypeSelected = onDataSourceTypeSelected;
            window._onBindingSelected = onBindingSelected;

            window.titleContent = new GUIContent("Add Binding");
            window.minSize = new Vector2(520, 460);
            window.position = GetCenteredPosition(520, 460);

            window.ShowUtility();
            window.Focus();
        }

        private static Rect GetCenteredPosition(float width, float height)
        {
            var main = EditorGUIUtility.GetMainWindowPosition();

            return new Rect(
                main.x + (main.width - width) * 0.5f,
                main.y + (main.height - height) * 0.5f,
                width,
                height
            );
        }

        private void CreateGUI()
        {
            rootVisualElement.style.paddingLeft = 8;
            rootVisualElement.style.paddingRight = 8;
            rootVisualElement.style.paddingTop = 8;
            rootVisualElement.style.paddingBottom = 8;

            BuildTypeChoices();

            _typeField = new PopupField<TypeChoice>(
                label: "Parent Source Type",
                choices: _typeChoices,
                defaultIndex: GetCurrentTypeChoiceIndex(),
                formatSelectedValueCallback: FormatTypeChoice,
                formatListItemCallback: FormatTypeChoice
            );

            _typeField.RegisterValueChangedCallback(evt =>
            {
                var selectedType = evt.newValue?.Type;

                _dataSourceType = selectedType;

                if (_dataSourceType != null)
                    _onDataSourceTypeSelected?.Invoke(_dataSourceType);

                RebuildEntries();
            });

            rootVisualElement.Add(_typeField);

            _helpBox = new HelpBox("", HelpBoxMessageType.Info)
            {
                style =
                {
                    marginTop = 6,
                    marginBottom = 6
                }
            };

            rootVisualElement.Add(_helpBox);

            _searchField = new TextField("Search")
            {
                isDelayed = false
            };

            _searchField.RegisterValueChangedCallback(evt => ApplyFilter(evt.newValue));
            rootVisualElement.Add(_searchField);
            
            _collectionIndexContainer = new VisualElement
            {
                name = "Collection Index Container",
                style =
                {
                    display = DisplayStyle.None,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            _collectionIndexField = new IntegerField("Element Index")
            {
                value = 0,
                style =
                {
                    flexGrow = 1
                }
            };

            _collectionIndexField.RegisterValueChangedCallback(_ =>
            {
                UpdateBindButtonState();
            });

            _dynamicCollectionIndexToggle = new Toggle("Dynamic")
            {
                value = false,
                tooltip =
                    "The element index will be provided dynamically at runtime.",
                style =
                {
                    marginLeft = 12,
                    flexShrink = 0
                }
            };

            _dynamicCollectionIndexToggle.RegisterValueChangedCallback(evt =>
            {
                SetDynamicCollectionIndex(evt.newValue);
                UpdateBindButtonState();
            });

            _collectionIndexContainer.Add(_collectionIndexField);
            _collectionIndexContainer.Add(_dynamicCollectionIndexToggle);

            rootVisualElement.Add(_collectionIndexContainer);

            _listView = new ListView
            {
                itemsSource = _filteredEntries,
                selectionType = SelectionType.Single,
                fixedItemHeight = 26,
                makeItem = MakeRow,
                bindItem = BindRow,
                style =
                {
                    flexGrow = 1,
                    marginTop = 8,
                    marginBottom = 8
                }
            };

            _listView.itemsChosen += chosen =>
            {
                var entry = chosen.OfType<Entry>().FirstOrDefault();
                Choose(entry);
            };

            _listView.selectionChanged += _ =>
            {
                UpdateCollectionIndexState();
                UpdateBindButtonState();
            };

            rootVisualElement.Add(_listView);

            var footer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd
                }
            };

            footer.Add(new Button(Close)
            {
                text = "Cancel"
            });

            _bindButton = new Button(() =>
            {
                Choose(_listView.selectedItem as Entry);
            })
            {
                text = "Bind"
            };

            footer.Add(_bindButton);
            rootVisualElement.Add(footer);

            RebuildEntries();

            if (_dataSourceType == null)
                _typeField.Focus();
            else
                _searchField.Focus();
        }
        
        private void UpdateCollectionIndexState()
        {
            if (_collectionIndexContainer == null)
                return;

            var isCollectionElement =
                _listView?.selectedItem is Entry
                {
                    IsCollectionElement: true
                };

            _collectionIndexContainer.style.display =
                isCollectionElement
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (!isCollectionElement)
                return;

            SetDynamicCollectionIndex(
                _dynamicCollectionIndexToggle.value);
        }
        
        private void SetDynamicCollectionIndex(bool dynamic)
        {
            if (_collectionIndexField == null)
                return;

            if (dynamic)
            {
                _collectionIndexField.SetValueWithoutNotify(
                    InputBinding.CollectionDynamic);

                _collectionIndexField.SetEnabled(false);
            }
            else
            {
                _collectionIndexField.SetEnabled(true);

                if (_collectionIndexField.value ==
                    InputBinding.CollectionDynamic)
                {
                    _collectionIndexField.SetValueWithoutNotify(0);
                }
            }
        }

        private void BuildTypeChoices()
        {
            _typeChoices.Clear();

            _typeChoices.Add(new TypeChoice(
                type: null,
                label: "Select parent source type.."
            ));

            var registeredTypes = ParentBindingRegistry.GetRegisteredTypes();

            foreach (var type in registeredTypes)
            {
                _typeChoices.Add(new TypeChoice(
                    type,
                    GetNiceTypeName(type)
                ));
            }

            if (_dataSourceType != null && !registeredTypes.Contains(_dataSourceType))
            {
                _typeChoices.Add(new TypeChoice(
                    _dataSourceType,
                    $"{GetNiceTypeName(_dataSourceType)} (not registered)"
                ));
            }
        }

        private int GetCurrentTypeChoiceIndex()
        {
            if (_dataSourceType == null)
                return 0;

            for (var i = 0; i < _typeChoices.Count; i++)
            {
                if (_typeChoices[i].Type == _dataSourceType)
                    return i;
            }

            return 0;
        }

        private static string FormatTypeChoice(TypeChoice choice)
        {
            return choice?.Label ?? "Select parent source type..";
        }

        private void RebuildEntries()
        {
            _allEntries.Clear();
            _filteredEntries.Clear();

            if (_dataSourceType != null)
            {
                var descriptors = ParentBindingRegistry.Get(_dataSourceType);

                foreach (var descriptor in descriptors)
                {
                    // Ordinary direct binding.
                    if (IsDirectlyCompatible(descriptor))
                    {
                        _allEntries.Add(new Entry(
                            descriptor,
                            descriptor.ValueType,
                            isCollectionElement: false));
                    }

                    // Collection element binding.
                    if (_expectedValueType != null &&
                        descriptor.ValueType != null &&
                        TryGetCollectionElementType(
                            descriptor.ValueType,
                            out var elementType) &&
                        IsTypeCompatible(_expectedValueType, elementType))
                    {
                        _allEntries.Add(new Entry(
                            descriptor,
                            elementType,
                            isCollectionElement: true));
                    }
                }

                _allEntries.Sort(static (a, b) =>
                    string.Compare(
                        a.Descriptor.Path,
                        b.Descriptor.Path,
                        StringComparison.Ordinal));
            }

            ApplyFilter(_searchField?.value ?? "");
            UpdateState();
        }

        private void ApplyFilter(string search)
        {
            _filteredEntries.Clear();

            search = search?.Trim() ?? "";

            foreach (var entry in _allEntries)
            {
                var descriptor = entry.Descriptor;

                if (search.Length > 0 &&
                    descriptor.Path.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) < 0 &&
                    GetNiceTypeName(entry.ResolvedValueType).IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                _filteredEntries.Add(entry);
            }

            _listView?.RefreshItems();
            UpdateState();
        }

        private bool IsDirectlyCompatible(
            ParentBindingDescriptor descriptor)
        {
            if (!_expectedKind.HasValue ||
                _expectedKind.Value == InputValueKind.None)
            {
                return true;
            }

            if (descriptor.Kind != _expectedKind.Value)
                return false;

            if (_expectedValueType == null)
                return true;

            if (descriptor.ValueType == null)
                return false;

            return IsTypeCompatible(
                expectedType: _expectedValueType,
                actualType: descriptor.ValueType);
        }
        
        private static bool IsTypeCompatible(Type expectedType, Type actualType)
        {
            expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
            actualType = Nullable.GetUnderlyingType(actualType) ?? actualType;

            if (expectedType == actualType)
                return true;

            return expectedType.IsAssignableFrom(actualType);
        }

        private void UpdateState()
        {
            var hasType = _dataSourceType != null;
            var hasRegisteredTypes = ParentBindingRegistry.GetRegisteredTypes().Length > 0;
            var hasEntries = _filteredEntries.Count > 0;

            _searchField?.SetEnabled(hasType);
            _listView?.SetEnabled(hasType && hasEntries);

            if (_helpBox != null)
            {
                if (!hasRegisteredTypes)
                {
                    _helpBox.text =
                        "No parent binding source types are registered. Register descriptors with ParentBindingRegistry.Register(...) first.";
                    _helpBox.messageType = HelpBoxMessageType.Warning;
                    _helpBox.style.display = DisplayStyle.Flex;
                }
                else if (!hasType)
                {
                    _helpBox.text =
                        "Select the parent dataSourceType before choosing a binding property.";
                    _helpBox.messageType = HelpBoxMessageType.Info;
                    _helpBox.style.display = DisplayStyle.Flex;
                }
                else if (!ParentBindingRegistry.IsRegistered(_dataSourceType))
                {
                    _helpBox.text =
                        $"'{_dataSourceType.FullName}' is not registered in ParentBindingRegistry.";
                    _helpBox.messageType = HelpBoxMessageType.Warning;
                    _helpBox.style.display = DisplayStyle.Flex;
                }
                else if (!hasEntries)
                {
                    _helpBox.text =
                        _expectedValueType != null
                            ? $"No compatible parent bindings were found for input type '{GetNiceTypeName(_expectedValueType)}'."
                            : "No compatible parent bindings were found for this input kind.";
                    
                    _helpBox.messageType = HelpBoxMessageType.Info;
                    _helpBox.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _helpBox.style.display = DisplayStyle.None;
                }
            }

            UpdateBindButtonState();
        }

        private void UpdateBindButtonState()
        {
            if (_bindButton == null)
                return;

            var entry = _listView?.selectedItem as Entry;

            var hasValidIndex =
                entry is not { IsCollectionElement: true } ||
                IsCollectionIndexValid();

            _bindButton.SetEnabled(
                _dataSourceType != null &&
                entry != null &&
                hasValidIndex);
        }

        private bool IsCollectionIndexValid()
        {
            if (_dynamicCollectionIndexToggle.value)
            {
                return _collectionIndexField.value ==
                       InputBinding.CollectionDynamic;
            }

            return _collectionIndexField.value >= 0;
        }

        private static VisualElement MakeRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingLeft = 4,
                    paddingRight = 4
                }
            };

            row.Add(new Label
            {
                name = "Path",
                style =
                {
                    flexGrow = 1,
                    unityTextAlign = TextAnchor.MiddleLeft
                }
            });

            row.Add(new Label
            {
                name = "Kind",
                style =
                {
                    width = 80,
                    opacity = 0.75f,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            });

            row.Add(new Label
            {
                name = "Type",
                style =
                {
                    width = 100,
                    opacity = 0.65f,
                    unityTextAlign = TextAnchor.MiddleRight
                }
            });

            return row;
        }

        private void BindRow(
            VisualElement row,
            int index)
        {
            var entry = _filteredEntries[index];

            row.Q<Label>("Path").text =
                entry.IsCollectionElement
                    ? $"{entry.Descriptor.Path}[index]"
                    : entry.Descriptor.Path;

            row.Q<Label>("Kind").text =
                entry.IsCollectionElement
                    ? "Element"
                    : entry.Descriptor.Kind.ToString();

            row.Q<Label>("Type").text =
                GetNiceTypeName(entry.ResolvedValueType);
        }

        private void Choose(Entry entry)
        {
            if (entry == null || _dataSourceType == null)
                return;

            int? collectionIndex = null;

            if (entry.IsCollectionElement)
            {
                if (!IsCollectionIndexValid())
                    return;

                collectionIndex = _dynamicCollectionIndexToggle.value
                    ? InputBinding.CollectionDynamic
                    : _collectionIndexField.value;
            }

            var selection = new ParentBindingSelection(
                entry.Descriptor,
                collectionIndex);

            _onBindingSelected?.Invoke(
                _dataSourceType,
                selection);

            Close();
        }

        private static string GetNiceTypeName(Type type)
        {
            if (type == null)
                return "";

            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(double)) return "double";
            if (type == typeof(long)) return "long";
            if (type == typeof(ulong)) return "ulong";

            return type.Name;
        }
        
        private static bool TryGetCollectionElementType(
            Type type,
            out Type elementType)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return elementType != null;
            }

            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();

                if (definition == typeof(List<>) ||
                    definition == typeof(IList<>) ||
                    definition == typeof(IReadOnlyList<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            foreach (var interfaceType in type.GetInterfaces())
            {
                if (!interfaceType.IsGenericType)
                    continue;

                var definition = interfaceType.GetGenericTypeDefinition();

                if (definition == typeof(IList<>) ||
                    definition == typeof(IReadOnlyList<>))
                {
                    elementType = interfaceType.GetGenericArguments()[0];
                    return true;
                }
            }

            elementType = null;
            return false;
        }

        private sealed class TypeChoice
        {
            public readonly Type Type;
            public readonly string Label;

            public TypeChoice(Type type, string label)
            {
                Type = type;
                Label = label;
            }
        }

        private sealed class Entry
        {
            public ParentBindingDescriptor Descriptor { get; }
            public Type ResolvedValueType { get; }
            public bool IsCollectionElement { get; }

            public Entry(
                ParentBindingDescriptor descriptor,
                Type resolvedValueType,
                bool isCollectionElement)
            {
                Descriptor = descriptor;
                ResolvedValueType = resolvedValueType;
                IsCollectionElement = isCollectionElement;
            }
        }
        
        public readonly struct ParentBindingSelection
        {
            public ParentBindingDescriptor Descriptor { get; }
            public int? CollectionIndex { get; }

            public ParentBindingSelection(
                ParentBindingDescriptor descriptor,
                int? collectionIndex = null)
            {
                Descriptor = descriptor;
                CollectionIndex = collectionIndex;
            }
        }
    }
    
    internal static class SerializedTypeUtility
    {
        internal static Type GetTypeValue(SerializedProperty property)
        {
            if (property == null)
                return null;

            // Type stored as System.Type
            if (property.propertyType != SerializedPropertyType.String)
            {
                try
                {
                    if (property.boxedValue is Type boxedType)
                        return boxedType;
                }
                catch
                {
                    // Fall through
                }

                if (property.propertyType == SerializedPropertyType.ManagedReference)
                    return property.managedReferenceValue as Type;
            }

            // Type stored as text
            if (property.propertyType == SerializedPropertyType.String)
            {
                var value = property.stringValue;

                if (string.IsNullOrWhiteSpace(value))
                    return null;

                return Type.GetType(value);
            }

            return null;
        }

        internal static void SetTypeValue(SerializedProperty property, Type type)
        {
            if (property == null)
                return;

            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = type == null
                    ? string.Empty
                    : $"{type.FullName}, {type.Assembly.GetName().Name}";

                return;
            }

            try
            {
                property.boxedValue = type;
                return;
            }
            catch
            {
                // Fall through
            }

            if (property.propertyType == SerializedPropertyType.ManagedReference)
                property.managedReferenceValue = type;
        }
    }
}