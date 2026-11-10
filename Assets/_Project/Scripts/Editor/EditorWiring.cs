using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossFight.EditorTools
{
    /// <summary>
    /// 给组件的 [SerializeField] private 字段赋值（相当于在 Inspector 里拖引用）。
    /// 字段名写错会直接抛异常，重命名字段后搭建脚本不会悄悄失效。
    /// </summary>
    internal static class EditorWiring
    {
        public static void SetRef(Object target, string field, Object value)
        {
            Edit(target, field, p => p.objectReferenceValue = value);
        }

        public static void Edit(Object target, string field, Action<SerializedProperty> edit)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"{target.GetType().Name} 上没有序列化字段 '{field}'");
            }

            edit(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>创建基础几何体并去掉自带的碰撞体（纯显示用）。</summary>
        public static GameObject CreateVisualPrimitive(PrimitiveType type, string name, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
    }
}
