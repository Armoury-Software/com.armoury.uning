/*using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Armoury.UI.Editor.Markers
{
    [CustomPropertyDrawer(typeof(DirectiveDefinition.UxmlSerializedData))]
    public sealed class DirectiveDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var typeProperty = property.FindPropertyRelative(nameof(DirectiveDefinition.Type));
            var inputsProperty = property.FindPropertyRelative(nameof(DirectiveDefinition.Inputs));

            if (typeProperty == null)
            {
                root.Add(new HelpBox(
                    $"Could not find serialized property '{nameof(DirectiveDefinition.Type)}'.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            if (inputsProperty == null)
            {
                root.Add(new HelpBox(
                    $"Could not find serialized property '{nameof(DirectiveDefinition.Inputs)}'.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            root.Add(new PropertyField(typeProperty, "Directive Type"));

            root.Add(InputListUi.Create(
                typeProperty,
                inputsProperty,
                selectedTypeName: "directive",
                selectTypeMessage: "Select a directive type first.",
                addButtonText: "Add Input"
            ));

            return root;
        }
    }

    [CustomPropertyDrawer(typeof(ComponentDefinition.UxmlSerializedData))]
    public sealed class ComponentDefinitionDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var typeProperty = property.FindPropertyRelative(nameof(ComponentDefinition.Type));
            var inputsProperty = property.FindPropertyRelative(nameof(ComponentDefinition.Inputs));

            if (typeProperty == null)
            {
                root.Add(new HelpBox(
                    $"Could not find serialized property '{nameof(ComponentDefinition.Type)}'.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            if (inputsProperty == null)
            {
                root.Add(new HelpBox(
                    $"Could not find serialized property '{nameof(ComponentDefinition.Inputs)}'. " +
                    "ComponentDefinition should expose a List<InputBase> named Inputs.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            root.Add(new PropertyField(typeProperty, "Component Type"));

            var validationContainer = new VisualElement();
            root.Add(validationContainer);

            void RebuildValidation()
            {
                typeProperty.serializedObject.Update();

                var freshTypeProperty = typeProperty.serializedObject.FindProperty(typeProperty.propertyPath);

                validationContainer.Clear();

                var componentType = ReadType(freshTypeProperty);

                if (componentType == null)
                {
                    validationContainer.Add(new HelpBox(
                        "A Component type is required.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }

                if (!typeof(Component).IsAssignableFrom(componentType))
                {
                    validationContainer.Add(new HelpBox(
                        $"'{componentType.FullName}' is not a Component. Select a type derived from Component.",
                        HelpBoxMessageType.Error
                    ));
                }
            }

            root.TrackPropertyValue(typeProperty, _ =>
            {
                root.schedule.Execute(RebuildValidation);
            });

            root.schedule.Execute(RebuildValidation);

            root.Add(InputListUi.Create(
                typeProperty,
                inputsProperty,
                selectedTypeName: "component",
                selectTypeMessage: "Select a component type first.",
                addButtonText: "Add Input"
            ));

            return root;
        }

        private static Type ReadType(SerializedProperty property)
        {
            if (property == null)
                return null;

            if (property.propertyType == SerializedPropertyType.String)
            {
                return string.IsNullOrWhiteSpace(property.stringValue)
                    ? null
                    : Type.GetType(property.stringValue, false);
            }

            try
            {
                return property.boxedValue as Type;
            }
            catch
            {
                return null;
            }
        }
    }


    [CustomPropertyDrawer(typeof(ComponentMarker.UxmlSerializedData))]
    public sealed class ComponentMarkerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();

            var directivesProperty = property.FindPropertyRelative(nameof(ElementMarker.Directives));
            var componentProperty = property.FindPropertyRelative(nameof(ComponentMarker.Component));

            if (directivesProperty != null)
                root.Add(new PropertyField(directivesProperty, "Directives"));

            if (componentProperty == null)
            {
                root.Add(new HelpBox(
                    $"Could not find serialized property '{nameof(ComponentMarker.Component)}'. " +
                    "ComponentMarker should expose a ComponentDefinition property named Component.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            var serializedObject = componentProperty.serializedObject;
            var componentPath = componentProperty.propertyPath;

            var componentContainer = new VisualElement();
            root.Add(componentContainer);

            void RebuildComponentUI()
            {
                serializedObject.Update();

                var freshComponentProperty = serializedObject.FindProperty(componentPath);

                componentContainer.Clear();

                if (freshComponentProperty == null)
                {
                    componentContainer.Add(new HelpBox(
                        "Could not rebuild the component UI because the Component property could not be found.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }

                componentContainer.Add(new PropertyField(freshComponentProperty, "Component"));

                if (!IsComponentDefinitionAssigned(freshComponentProperty))
                {
                    componentContainer.Add(new HelpBox(
                        "ComponentMarker requires a ComponentDefinition with a valid Component type.",
                        HelpBoxMessageType.Error
                    ));

                    componentContainer.Add(new Button(() =>
                    {
                        CreateComponentDefinition(serializedObject, componentPath);
                        componentContainer.schedule.Execute(RebuildComponentUI);
                    })
                    {
                        text = "Create Component Definition"
                    });

                    return;
                }

                var componentTypeProperty = freshComponentProperty.FindPropertyRelative(nameof(ComponentDefinition.Type));

                if (componentTypeProperty == null)
                {
                    componentContainer.Add(new HelpBox(
                        $"The assigned ComponentDefinition is missing its '{nameof(ComponentDefinition.Type)}' property.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }

                var componentType = ReadType(componentTypeProperty);

                if (componentType == null)
                {
                    componentContainer.Add(new HelpBox(
                        "ComponentMarker requires the assigned ComponentDefinition to have a valid Component type selected.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }

                if (!typeof(Component).IsAssignableFrom(componentType))
                {
                    componentContainer.Add(new HelpBox(
                        $"'{componentType.FullName}' is not a Component. ComponentMarker only accepts types derived from Component.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }
            }

            root.TrackPropertyValue(componentProperty, _ =>
            {
                root.schedule.Execute(RebuildComponentUI);
            });

            root.schedule.Execute(RebuildComponentUI);
            return root;
        }

        private static bool IsComponentDefinitionAssigned(SerializedProperty componentProperty)
        {
            if (componentProperty == null)
                return false;

            if (componentProperty.propertyType == SerializedPropertyType.ManagedReference)
                return componentProperty.managedReferenceValue != null;

            return componentProperty.FindPropertyRelative(nameof(ComponentDefinition.Type)) != null ||
                   componentProperty.FindPropertyRelative(nameof(ComponentDefinition.Inputs)) != null;
        }

        private static void CreateComponentDefinition(
            SerializedObject serializedObject,
            string componentPath
        )
        {
            serializedObject.Update();

            var componentProperty = serializedObject.FindProperty(componentPath);

            if (componentProperty == null)
            {
                Debug.LogError("Could not create ComponentDefinition because the Component property could not be found.");
                return;
            }

            if (componentProperty.propertyType != SerializedPropertyType.ManagedReference)
            {
                Debug.LogError(
                    "Could not create ComponentDefinition automatically because the Component property is not a managed reference. " +
                    "Use [UxmlObjectReference] on ComponentMarker.Component."
                );
                return;
            }

            componentProperty.managedReferenceValue =
                UxmlSerializedDataCreator.CreateUxmlSerializedData(typeof(ComponentDefinition));

            serializedObject.ApplyModifiedProperties();
        }

        private static Type ReadType(SerializedProperty property)
        {
            if (property == null)
                return null;

            if (property.propertyType == SerializedPropertyType.String)
            {
                return string.IsNullOrWhiteSpace(property.stringValue)
                    ? null
                    : Type.GetType(property.stringValue, false);
            }

            try
            {
                return property.boxedValue as Type;
            }
            catch
            {
                return null;
            }
        }
    }

    internal static class InputListUi
    {
        private const string ValuePropertyName = "Value";

        private sealed class InputMember
        {
            public string Name { get; }
            public Type ValueType { get; }
            public MemberInfo MemberInfo { get; }

            public InputMember(FieldInfo field)
            {
                Name = field.Name;
                ValueType = field.FieldType;
                MemberInfo = field;
            }

            public InputMember(PropertyInfo property)
            {
                Name = property.Name;
                ValueType = property.PropertyType;
                MemberInfo = property;
            }
        }

        private static readonly Dictionary<Type, List<InputMember>> InputMembersCache = new();
        private static readonly Dictionary<Type, Type> UxmlSerializedDataTypeCache = new();

        public static VisualElement Create(
            SerializedProperty typeProperty,
            SerializedProperty inputsProperty,
            string selectedTypeName,
            string selectTypeMessage,
            string addButtonText
        )
        {
            var root = new VisualElement();

            if (typeProperty == null || inputsProperty == null)
            {
                root.Add(new HelpBox(
                    "Could not create input UI because one or more serialized properties are missing.",
                    HelpBoxMessageType.Error
                ));
                return root;
            }

            var serializedObject = typeProperty.serializedObject;
            var typePath = typeProperty.propertyPath;
            var inputsPath = inputsProperty.propertyPath;

            var inputsContainer = new VisualElement();
            root.Add(inputsContainer);

            void RebuildInputsUI()
            {
                serializedObject.Update();

                var freshTypeProperty = serializedObject.FindProperty(typePath);
                var freshInputsProperty = serializedObject.FindProperty(inputsPath);

                inputsContainer.Clear();

                if (freshTypeProperty == null || freshInputsProperty == null)
                {
                    inputsContainer.Add(new HelpBox(
                        "Could not rebuild inputs because the serialized properties could not be found.",
                        HelpBoxMessageType.Error
                    ));
                    return;
                }

                var selectedType = ReadType(freshTypeProperty);

                if (selectedType == null)
                {
                    inputsContainer.Add(new HelpBox(
                        selectTypeMessage,
                        HelpBoxMessageType.Info
                    ));
                    return;
                }

                var inputMembers = GetInputMembers(selectedType);

                if (inputMembers.Count == 0)
                {
                    if (freshInputsProperty.arraySize > 0)
                    {
                        Debug.LogError(
                            $"'{selectedType.FullName}' has no fields/properties marked with InputAttribute. Removing its configured inputs."
                        );

                        freshInputsProperty.ClearArray();
                        serializedObject.ApplyModifiedProperties();
                    }

                    if (!typeof(Component).IsAssignableFrom(selectedType))
                    {
                        inputsContainer.Add(new HelpBox(
                            $"The selected {selectedTypeName} has no input field/propertys/properties.",
                            HelpBoxMessageType.Error
                        ));
                    }

                    return;
                }

                if (NormalizeInputsList(serializedObject, inputsPath, selectedType, inputMembers))
                {
                    serializedObject.Update();
                    freshInputsProperty = serializedObject.FindProperty(inputsPath);

                    if (freshInputsProperty == null)
                    {
                        inputsContainer.Add(new HelpBox(
                            "Could not rebuild inputs because the inputs property disappeared after normalization.",
                            HelpBoxMessageType.Error
                        ));
                        return;
                    }
                }

                for (var i = 0; i < freshInputsProperty.arraySize; i++)
                {
                    DrawInputItem(
                        inputsContainer,
                        serializedObject,
                        inputsPath,
                        i,
                        selectedType,
                        inputMembers,
                        RebuildInputsUI
                    );
                }

                var addButton = new Button(() =>
                {
                    serializedObject.Update();

                    var latestTypeProperty = serializedObject.FindProperty(typePath);
                    var latestInputsProperty = serializedObject.FindProperty(inputsPath);

                    if (latestTypeProperty == null || latestInputsProperty == null)
                    {
                        Debug.LogError("Could not add input because the serialized properties could not be found.");
                        return;
                    }

                    var latestSelectedType = ReadType(latestTypeProperty);

                    if (latestSelectedType == null)
                    {
                        Debug.LogError(selectTypeMessage);
                        return;
                    }

                    var latestInputMembers = GetInputMembers(latestSelectedType);

                    if (latestInputMembers.Count == 0)
                    {
                        Debug.LogError(
                            $"'{latestSelectedType.FullName}' has no fields/properties marked with InputAttribute."
                        );
                        return;
                    }

                    var selectedField = FindFirstSupportedInputMember(latestInputMembers);

                    if (selectedField == null)
                    {
                        Debug.LogError(
                            $"'{latestSelectedType.FullName}' has input field/propertys/properties, but none of them use a supported value type. " +
                            "Supported types are string, int, float, bool, and UnityEngine.Object-derived types."
                        );
                        return;
                    }

                    AddInput(serializedObject, inputsPath, selectedField);
                    inputsContainer.schedule.Execute(RebuildInputsUI);
                })
                {
                    text = addButtonText
                };

                inputsContainer.Add(addButton);
            }

            root.TrackPropertyValue(typeProperty, _ =>
            {
                root.schedule.Execute(RebuildInputsUI);
            });

            root.schedule.Execute(RebuildInputsUI);
            return root;
        }

        private static void DrawInputItem(
            VisualElement parent,
            SerializedObject serializedObject,
            string inputsPath,
            int index,
            Type selectedType,
            List<InputMember> inputMembers,
            Action rebuild
        )
        {
            serializedObject.Update();

            var inputsProperty = serializedObject.FindProperty(inputsPath);

            if (inputsProperty == null || index < 0 || index >= inputsProperty.arraySize)
                return;

            var item = inputsProperty.GetArrayElementAtIndex(index);

            var nameProperty = item.FindPropertyRelative(nameof(InputBase.Name));
            var fieldTypeNameProperty = item.FindPropertyRelative(nameof(InputBase.FieldTypeName));

            if (nameProperty == null || fieldTypeNameProperty == null)
            {
                parent.Add(new HelpBox(
                    "Could not draw this input because one or more InputBase serialized properties could not be found.",
                    HelpBoxMessageType.Error
                ));
                return;
            }

            var box = new Box();
            box.style.marginTop = 3;
            box.style.marginBottom = 3;
            box.style.paddingTop = 4;
            box.style.paddingBottom = 4;
            box.style.paddingLeft = 4;
            box.style.paddingRight = 4;

            var choices = new List<string>(inputMembers.Count);
            foreach (var field in inputMembers)
                choices.Add(field.Name);

            var currentName = nameProperty.stringValue;
            var currentIndex = choices.IndexOf(currentName);
            if (currentIndex < 0)
                currentIndex = 0;

            var inputPopup = new PopupField<string>(
                "Input",
                choices,
                currentIndex
            );

            inputPopup.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();

                var selectedField = FindInputMember(selectedType, evt.newValue);

                if (selectedField == null)
                {
                    Debug.LogError(
                        $"'{evt.newValue}' is not a valid input field/property/property on '{selectedType.FullName}'."
                    );

                    var freshInputsProperty = serializedObject.FindProperty(inputsPath);
                    DeleteArrayElementSafely(freshInputsProperty, index);
                    serializedObject.ApplyModifiedProperties();

                    parent.schedule.Execute(rebuild);
                    return;
                }

                if (!IsSupportedInputMemberType(selectedField.ValueType))
                {
                    Debug.LogError(
                        $"Input field/property '{selectedField.Name}' on '{selectedType.FullName}' has unsupported type " +
                        $"'{selectedField.ValueType.FullName}'. Supported types are string, int, float, bool, " +
                        "and UnityEngine.Object-derived types."
                    );

                    var freshInputsProperty = serializedObject.FindProperty(inputsPath);
                    DeleteArrayElementSafely(freshInputsProperty, index);
                    serializedObject.ApplyModifiedProperties();

                    parent.schedule.Execute(rebuild);
                    return;
                }

                ReplaceInputItem(serializedObject, inputsPath, index, selectedField, resetValue: true);
                parent.schedule.Execute(rebuild);
            });

            box.Add(inputPopup);

            var selectedInputField = FindInputMember(selectedType, nameProperty.stringValue);

            if (selectedInputField == null)
            {
                box.Add(new HelpBox(
                    $"Input '{nameProperty.stringValue}' no longer exists on '{selectedType.FullName}' and will be removed.",
                    HelpBoxMessageType.Error
                ));

                box.schedule.Execute(() =>
                {
                    serializedObject.Update();

                    var freshInputsProperty = serializedObject.FindProperty(inputsPath);
                    DeleteArrayElementSafely(freshInputsProperty, index);

                    serializedObject.ApplyModifiedProperties();
                    parent.schedule.Execute(rebuild);
                });

                parent.Add(box);
                return;
            }

            if (!IsSupportedInputMemberType(selectedInputField.ValueType))
            {
                box.Add(new HelpBox(
                    $"Input '{selectedInputField.Name}' has unsupported type '{selectedInputField.ValueType.FullName}' and will be removed.",
                    HelpBoxMessageType.Error
                ));

                box.schedule.Execute(() =>
                {
                    serializedObject.Update();

                    var freshInputsProperty = serializedObject.FindProperty(inputsPath);
                    DeleteArrayElementSafely(freshInputsProperty, index);

                    serializedObject.ApplyModifiedProperties();
                    parent.schedule.Execute(rebuild);
                });

                parent.Add(box);
                return;
            }

            var valueProperty = item.FindPropertyRelative(ValuePropertyName);

            if (valueProperty == null)
            {
                box.Add(new HelpBox(
                    $"Could not find value property '{ValuePropertyName}' for input '{selectedInputField.Name}'. " +
                    "Make sure your concrete input UXML object has a public property named Value marked with [UxmlAttribute].",
                    HelpBoxMessageType.Error
                ));

                parent.Add(box);
                return;
            }

            box.Add(CreateValueField(selectedInputField.ValueType, valueProperty));

            var removeButton = new Button(() =>
            {
                serializedObject.Update();

                var freshInputsProperty = serializedObject.FindProperty(inputsPath);
                DeleteArrayElementSafely(freshInputsProperty, index);

                serializedObject.ApplyModifiedProperties();
                parent.schedule.Execute(rebuild);
            })
            {
                text = "Remove"
            };

            box.Add(removeButton);
            parent.Add(box);
        }

        private static VisualElement CreateValueField(
            Type fieldType,
            SerializedProperty valueProperty
        )
        {
            var serializedObject = valueProperty.serializedObject;
            var valuePath = valueProperty.propertyPath;

            if (fieldType == typeof(string))
            {
                var field = new TextField("Value");
                field.SetValueWithoutNotify(valueProperty.stringValue);

                field.RegisterValueChangedCallback(evt =>
                {
                    serializedObject.Update();

                    var freshValueProperty = serializedObject.FindProperty(valuePath);
                    if (freshValueProperty == null)
                        return;

                    freshValueProperty.stringValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                return field;
            }

            if (fieldType == typeof(int))
            {
                var field = new IntegerField("Value");
                field.SetValueWithoutNotify(valueProperty.intValue);

                field.RegisterValueChangedCallback(evt =>
                {
                    serializedObject.Update();

                    var freshValueProperty = serializedObject.FindProperty(valuePath);
                    if (freshValueProperty == null)
                        return;

                    freshValueProperty.intValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                return field;
            }

            if (fieldType == typeof(float))
            {
                var field = new FloatField("Value");
                field.SetValueWithoutNotify(valueProperty.floatValue);

                field.RegisterValueChangedCallback(evt =>
                {
                    serializedObject.Update();

                    var freshValueProperty = serializedObject.FindProperty(valuePath);
                    if (freshValueProperty == null)
                        return;

                    freshValueProperty.floatValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                return field;
            }

            if (fieldType == typeof(bool))
            {
                var field = new Toggle("Value");
                field.SetValueWithoutNotify(valueProperty.boolValue);

                field.RegisterValueChangedCallback(evt =>
                {
                    serializedObject.Update();

                    var freshValueProperty = serializedObject.FindProperty(valuePath);
                    if (freshValueProperty == null)
                        return;

                    freshValueProperty.boolValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                return field;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            {
                var field = new ObjectField("Value")
                {
                    objectType = fieldType,
                    allowSceneObjects = false
                };

                field.SetValueWithoutNotify(valueProperty.objectReferenceValue);

                field.RegisterValueChangedCallback(evt =>
                {
                    serializedObject.Update();

                    var freshValueProperty = serializedObject.FindProperty(valuePath);
                    if (freshValueProperty == null)
                        return;

                    freshValueProperty.objectReferenceValue = evt.newValue;
                    serializedObject.ApplyModifiedProperties();
                });

                return field;
            }

            return new HelpBox(
                $"Unsupported input value type: {fieldType.FullName}",
                HelpBoxMessageType.Warning
            );
        }

        private static bool NormalizeInputsList(
            SerializedObject serializedObject,
            string inputsPath,
            Type selectedType,
            List<InputMember> inputMembers
        )
        {
            serializedObject.Update();

            var inputsProperty = serializedObject.FindProperty(inputsPath);

            if (inputsProperty == null)
                return false;

            var changed = false;

            for (var i = inputsProperty.arraySize - 1; i >= 0; i--)
            {
                var item = inputsProperty.GetArrayElementAtIndex(i);

                var nameProperty = item.FindPropertyRelative(nameof(InputBase.Name));

                if (nameProperty == null)
                {
                    Debug.LogError("An input item is missing its Name property and will be removed.");
                    DeleteArrayElementSafely(inputsProperty, i);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    inputsProperty = serializedObject.FindProperty(inputsPath);
                    changed = true;
                    continue;
                }

                var selectedField = FindInputMember(selectedType, nameProperty.stringValue);

                if (selectedField == null)
                {
                    if (string.IsNullOrWhiteSpace(nameProperty.stringValue))
                    {
                        selectedField = FindFirstSupportedInputMember(inputMembers);

                        if (selectedField == null)
                        {
                            Debug.LogError(
                                $"'{selectedType.FullName}' has input field/propertys/properties, but none of them use a supported value type. " +
                                "Supported types are string, int, float, bool, and UnityEngine.Object-derived types."
                            );

                            DeleteArrayElementSafely(inputsProperty, i);
                            serializedObject.ApplyModifiedProperties();
                            serializedObject.Update();
                            inputsProperty = serializedObject.FindProperty(inputsPath);
                            changed = true;
                            continue;
                        }

                        ReplaceInputItem(serializedObject, inputsPath, i, selectedField, resetValue: true);
                        serializedObject.Update();
                        inputsProperty = serializedObject.FindProperty(inputsPath);
                        changed = true;
                    }
                    else
                    {
                        Debug.LogError(
                            $"Input '{nameProperty.stringValue}' no longer exists on '{selectedType.FullName}' and will be removed."
                        );

                        DeleteArrayElementSafely(inputsProperty, i);
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.Update();
                        inputsProperty = serializedObject.FindProperty(inputsPath);
                        changed = true;
                    }

                    continue;
                }

                if (!IsSupportedInputMemberType(selectedField.ValueType))
                {
                    Debug.LogError(
                        $"Input '{selectedField.Name}' on '{selectedType.FullName}' has unsupported type " +
                        $"'{selectedField.ValueType.FullName}' and will be removed."
                    );

                    DeleteArrayElementSafely(inputsProperty, i);
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    inputsProperty = serializedObject.FindProperty(inputsPath);
                    changed = true;
                    continue;
                }

                if (!InputItemMatchesExpectedType(item, selectedField))
                {
                    ReplaceInputItem(serializedObject, inputsPath, i, selectedField, resetValue: true);
                    serializedObject.Update();
                    inputsProperty = serializedObject.FindProperty(inputsPath);
                    changed = true;
                    continue;
                }

                if (EnsureInputItemMetadataMatchesMember(item, selectedField))
                {
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                    inputsProperty = serializedObject.FindProperty(inputsPath);
                    changed = true;
                }
            }

            return changed;
        }

        private static void AddInput(
            SerializedObject serializedObject,
            string inputsPath,
            InputMember selectedField
        )
        {
            serializedObject.Update();

            var inputsProperty = serializedObject.FindProperty(inputsPath);

            if (inputsProperty == null)
            {
                Debug.LogError("Could not add input because the inputs property could not be found.");
                return;
            }

            if (!IsSupportedInputMemberType(selectedField.ValueType))
            {
                Debug.LogError(
                    $"Could not add input '{selectedField.Name}' because its type '{selectedField.ValueType.FullName}' is unsupported."
                );
                return;
            }

            var index = inputsProperty.arraySize;
            inputsProperty.InsertArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            ReplaceInputItem(serializedObject, inputsPath, index, selectedField, resetValue: true);
        }

        private static bool ReplaceInputItem(
            SerializedObject serializedObject,
            string inputsPath,
            int index,
            InputMember selectedField,
            bool resetValue
        )
        {
            var inputUxmlObjectType = GetInputUxmlObjectType(selectedField.ValueType);

            if (inputUxmlObjectType == null)
            {
                Debug.LogError(
                    $"Input field/property '{selectedField.Name}' has unsupported type '{selectedField.ValueType.FullName}'."
                );
                return false;
            }

            serializedObject.Update();

            var inputsProperty = serializedObject.FindProperty(inputsPath);

            if (inputsProperty == null)
                return false;

            if (index < 0 || index >= inputsProperty.arraySize)
                return false;

            var item = inputsProperty.GetArrayElementAtIndex(index);

            item.managedReferenceValue =
                UxmlSerializedDataCreator.CreateUxmlSerializedData(inputUxmlObjectType);

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();

            inputsProperty = serializedObject.FindProperty(inputsPath);

            if (inputsProperty == null || index < 0 || index >= inputsProperty.arraySize)
                return false;

            item = inputsProperty.GetArrayElementAtIndex(index);

            var nameProperty = item.FindPropertyRelative(nameof(InputBase.Name));
            var fieldTypeNameProperty = item.FindPropertyRelative(nameof(InputBase.FieldTypeName));
            var valueProperty = item.FindPropertyRelative(ValuePropertyName);

            if (nameProperty == null || fieldTypeNameProperty == null || valueProperty == null)
            {
                Debug.LogError(
                    $"Could not initialize input '{selectedField.Name}'. Make sure '{inputUxmlObjectType.Name}' " +
                    $"inherits InputBase and has a public property named {ValuePropertyName} marked with [UxmlAttribute]."
                );
                return false;
            }

            nameProperty.stringValue = selectedField.Name;
            fieldTypeNameProperty.stringValue = selectedField.ValueType.AssemblyQualifiedName;

            if (resetValue)
                SetDefaultValue(valueProperty, selectedField.ValueType);

            serializedObject.ApplyModifiedProperties();
            return true;
        }

        private static bool EnsureInputItemMetadataMatchesMember(
            SerializedProperty item,
            InputMember selectedField
        )
        {
            var nameProperty = item.FindPropertyRelative(nameof(InputBase.Name));
            var fieldTypeNameProperty = item.FindPropertyRelative(nameof(InputBase.FieldTypeName));

            if (nameProperty == null || fieldTypeNameProperty == null)
                return false;

            var changed = false;

            if (nameProperty.stringValue != selectedField.Name)
            {
                nameProperty.stringValue = selectedField.Name;
                changed = true;
            }

            var expectedFieldTypeName = selectedField.ValueType.AssemblyQualifiedName;

            if (fieldTypeNameProperty.stringValue != expectedFieldTypeName)
            {
                fieldTypeNameProperty.stringValue = expectedFieldTypeName;
                changed = true;
            }

            return changed;
        }

        private static bool InputItemMatchesExpectedType(
            SerializedProperty item,
            InputMember selectedField
        )
        {
            if (item == null || item.managedReferenceValue == null)
                return false;

            var inputUxmlObjectType = GetInputUxmlObjectType(selectedField.ValueType);
            var expectedSerializedDataType = GetUxmlSerializedDataType(inputUxmlObjectType);

            if (expectedSerializedDataType == null)
                return false;

            return expectedSerializedDataType.IsInstanceOfType(item.managedReferenceValue);
        }

        private static void SetDefaultValue(
            SerializedProperty valueProperty,
            Type fieldType
        )
        {
            if (valueProperty == null)
                return;

            if (fieldType == typeof(string))
            {
                valueProperty.stringValue = string.Empty;
                return;
            }

            if (fieldType == typeof(int))
            {
                valueProperty.intValue = 0;
                return;
            }

            if (fieldType == typeof(float))
            {
                valueProperty.floatValue = 0f;
                return;
            }

            if (fieldType == typeof(bool))
            {
                valueProperty.boolValue = false;
                return;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                valueProperty.objectReferenceValue = null;
        }

        private static InputMember FindFirstSupportedInputMember(List<InputMember> fields)
        {
            foreach (var field in fields)
            {
                if (IsSupportedInputMemberType(field.ValueType))
                    return field;
            }

            return null;
        }

        private static bool IsSupportedInputMemberType(Type fieldType)
        {
            return GetInputUxmlObjectType(fieldType) != null;
        }

        private static Type GetInputUxmlObjectType(Type fieldType)
        {
            if (fieldType == typeof(string))
                return typeof(StringInput);

            if (fieldType == typeof(int))
                return typeof(IntInput);

            if (fieldType == typeof(float))
                return typeof(FloatInput);

            if (fieldType == typeof(bool))
                return typeof(BoolInput);

            if (fieldType != null && typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
                return typeof(ObjectInput);

            return null;
        }

        private static Type GetUxmlSerializedDataType(Type uxmlObjectType)
        {
            if (uxmlObjectType == null)
                return null;

            if (UxmlSerializedDataTypeCache.TryGetValue(uxmlObjectType, out var cached))
                return cached;

            var serializedDataType = uxmlObjectType.GetNestedType(
                "UxmlSerializedData",
                BindingFlags.Public | BindingFlags.NonPublic
            );

            UxmlSerializedDataTypeCache[uxmlObjectType] = serializedDataType;
            return serializedDataType;
        }

        private static InputMember FindInputMember(Type selectedType, string fieldName)
        {
            if (selectedType == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            foreach (var field in GetInputMembers(selectedType))
            {
                if (field.Name == fieldName)
                    return field;
            }

            return null;
        }

        private static List<InputMember> GetInputMembers(Type selectedType)
        {
            if (selectedType == null)
                return new List<InputMember>();

            if (InputMembersCache.TryGetValue(selectedType, out var cached))
                return cached;

            var result = new List<InputMember>();

            for (
                var current = selectedType;
                current != null && typeof(Directive).IsAssignableFrom(current);
                current = current.BaseType
            )
            {
                var fields = current.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                foreach (var field in fields)
                {
                    if (field.GetCustomAttribute<InputAttribute>(true) == null)
                        continue;

                    if (ContainsInputMemberNamed(result, field.Name))
                        continue;

                    result.Add(new InputMember(field));
                }

                var properties = current.GetProperties(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<InputAttribute>(true) == null)
                        continue;

                    if (property.GetIndexParameters().Length > 0)
                    {
                        Debug.LogError(
                            $"Input property '{property.Name}' on '{current.FullName}' is an indexer and will be ignored."
                        );
                        continue;
                    }

                    if (!property.CanRead || !property.CanWrite)
                    {
                        Debug.LogError(
                            $"Input property '{property.Name}' on '{current.FullName}' must have both a getter and a setter."
                        );
                        continue;
                    }

                    if (ContainsInputMemberNamed(result, property.Name))
                        continue;

                    result.Add(new InputMember(property));
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            InputMembersCache[selectedType] = result;

            return result;
        }

        private static bool ContainsInputMemberNamed(List<InputMember> members, string name)
        {
            foreach (var member in members)
            {
                if (member.Name == name)
                    return true;
            }

            return false;
        }

        private static Type ReadType(SerializedProperty property)
        {
            if (property == null)
                return null;

            if (property.propertyType == SerializedPropertyType.String)
            {
                return string.IsNullOrWhiteSpace(property.stringValue)
                    ? null
                    : Type.GetType(property.stringValue, false);
            }

            try
            {
                return property.boxedValue as Type;
            }
            catch
            {
                return null;
            }
        }

        private static void DeleteArrayElementSafely(SerializedProperty arrayProperty, int index)
        {
            if (arrayProperty == null)
                return;

            if (index < 0 || index >= arrayProperty.arraySize)
                return;

            var oldSize = arrayProperty.arraySize;

            arrayProperty.DeleteArrayElementAtIndex(index);

            // Some Unity serialized array/list cases first null the element,
            // then require a second delete to actually shrink the list.
            if (arrayProperty.arraySize == oldSize && index < arrayProperty.arraySize)
                arrayProperty.DeleteArrayElementAtIndex(index);
        }
    }
}*/