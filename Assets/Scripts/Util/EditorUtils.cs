using System.Reflection;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public enum ConditionOperator
{
    And,
    Or,
}

public enum ActionOnConditionFail
{
    DontDraw, // If condition false, don't draw the field at all
    JustDisable // If conditions are false, set the field as disabled
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class ShowIfAttribute : PropertyAttribute
{
    public ActionOnConditionFail Action { get; private set; }
    public ConditionOperator Operator { get; private set; }
    public string[] Conditions { get; private set; }

    public ShowIfAttribute(ActionOnConditionFail action, ConditionOperator conditionOperator, params string[] conditions) {
        Action = action;
        Operator = conditionOperator;
        Conditions = conditions;
    }
}

// Adds an attribute you can use to only show a drawer in the editor if certain conditions are true.
// You can use it to make a boolean field hide subfields.
// Example 1: 
// showHide: true -> stringField shows in the editor.
// showHide: false -> stringField will not show in the editor.
// -----------------
// public bool showHide = false;
// [ShowIf(ActionOnConditionFail.DontDraw, ConditionOperator.And, nameof(showHide)]
// public string stringField = "item 1";
// -----------------
// Example 2:
// Same as above but now both showHide1 and showHide2 must be true.
// -----------------
// public bool showHide1 = false;
// public bool showHide2 = false;
// [ShowIf(ActionOnConditionFail.JustDisable, ConditionOperator.And, nameof(showHide1), nameOf(showHide2)]
// public string stringField = "item 1";
// -----------------
// Note you could also use ConditionOperator.Or to specify showHide1 OR showHide2.
// See https://stackoverflow.com/a/58446816 for more usage examples.
[CustomPropertyDrawer(typeof(ShowIfAttribute), true)]
public class ShowIfAttributeDrawer : PropertyDrawer
{
    #region Reflection helpers.
    private static MethodInfo GetMethod(object target, string methodName) {
        return GetAllMethods(target, m => m.Name.Equals(methodName,
                  StringComparison.InvariantCulture)).FirstOrDefault();
    }

    private static FieldInfo GetField(object target, string fieldName) {
        return GetAllFields(target, f => f.Name.Equals(fieldName,
              StringComparison.InvariantCulture)).FirstOrDefault();
    }
    private static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo,
            bool> predicate) {
        List<Type> types = new List<Type>()
            {
                target.GetType()
            };

        while (types.Last().BaseType != null) {
            types.Add(types.Last().BaseType);
        }

        for (int i = types.Count - 1; i >= 0; i--) {
            IEnumerable<FieldInfo> fieldInfos = types[i]
                .GetFields(BindingFlags.Instance | BindingFlags.Static |
   BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(predicate);

            foreach (var fieldInfo in fieldInfos) {
                yield return fieldInfo;
            }
        }
    }
    private static IEnumerable<MethodInfo> GetAllMethods(object target,
  Func<MethodInfo, bool> predicate) {
        IEnumerable<MethodInfo> methodInfos = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static |
  BindingFlags.NonPublic | BindingFlags.Public)
            .Where(predicate);

        return methodInfos;
    }
    #endregion

    private bool MeetsConditions(SerializedProperty property) {
        var showIfAttribute = this.attribute as ShowIfAttribute;
        var target = property.serializedObject.targetObject;
        List<bool> conditionValues = new List<bool>();

        foreach (var condition in showIfAttribute.Conditions) {
            FieldInfo conditionField = GetField(target, condition);
            if (conditionField != null &&
                conditionField.FieldType == typeof(bool)) {
                conditionValues.Add((bool)conditionField.GetValue(target));
            }

            MethodInfo conditionMethod = GetMethod(target, condition);
            if (conditionMethod != null &&
                conditionMethod.ReturnType == typeof(bool) &&
                conditionMethod.GetParameters().Length == 0) {
                conditionValues.Add((bool)conditionMethod.Invoke(target, null));
            }
        }

        if (conditionValues.Count > 0) {
            bool met;
            if (showIfAttribute.Operator == ConditionOperator.And) {
                met = true;
                foreach (var value in conditionValues) {
                    met = met && value;
                }
            } else {
                met = false;
                foreach (var value in conditionValues) {
                    met = met || value;
                }
            }
            return met;
        } else {
            Debug.LogError("Invalid boolean condition fields or methods used!");
            return true;
        }
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent
                 label) {
        // Calcluate the property height, if we don't meet the condition and the draw 
        // mode is DontDraw, then height will be 0.
        bool meetsCondition = MeetsConditions(property);
        var showIfAttribute = this.attribute as ShowIfAttribute;

        if (!meetsCondition && showIfAttribute.Action == ActionOnConditionFail.DontDraw) {
            return 0;
        }

        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent
           label) {
        bool meetsCondition = MeetsConditions(property);
        // Early out, if conditions met, draw and go.
        if (meetsCondition) {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        var showIfAttribute = this.attribute as ShowIfAttribute;
        if (showIfAttribute.Action == ActionOnConditionFail.DontDraw) {
            return;
        } else if (showIfAttribute.Action == ActionOnConditionFail.JustDisable) {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndDisabledGroup();
        }

    }
}