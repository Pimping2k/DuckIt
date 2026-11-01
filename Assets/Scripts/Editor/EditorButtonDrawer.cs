using System;
using System.Reflection;
using Data;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(EditorButtonAttribute))]
    public class EditorButtonDrawer : PropertyDrawer
    {
        private const BindingFlags Flags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight + 4f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (EditorButtonAttribute)attribute;
            var text = attr.Label ?? ObjectNames.NicifyVariableName(attr.Method);

            position.height -= 4f;
            if (!GUI.Button(position, text)) return;

            foreach (var target in property.serializedObject.targetObjects)
                Invoke(target, attr.Method);
        }

        private static void Invoke(object target, string methodName)
        {
            MethodInfo method = null;
            for (var t = target.GetType(); t != null && method == null; t = t.BaseType)
                method = t.GetMethod(methodName, Flags | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);

            if (method == null)
            {
                Debug.LogError($"[EditorButton] Parameterless method '{methodName}' not found in {target.GetType().Name}");
                return;
            }

            method.Invoke(target, null);
        }
    }
}