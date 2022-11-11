using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Ubiq.Avatars
{
    public abstract class AvatarHintProvider : MonoBehaviour
    {
        public virtual Vector3 ProvideVector3() { return Vector3.zero; }
        public virtual Quaternion ProvideQuaternion() { return Quaternion.identity; }
        public virtual float ProvideFloat() { return 0.0f; }
        public virtual string ProvideString() { return string.Empty; }
    }

    [System.Serializable]
    public class AvatarHints
    {
        [System.Serializable]
        private class SerializedAvatarHints
        {
            [System.Serializable]
            public class Instruction
            {
                [SerializeField] private string _node;
                [SerializeField] private TypedProvider _provider;

                public string node { get { return _node; } }
                public TypedProvider provider { get { return _provider; } }

                public Instruction(string node, TypedProvider provider)
                {
                    this._node = node;
                    this._provider = provider;
                }
            }

            [SerializeField] private List<Instruction> instructions;

            public void Deserialize (Dictionary<string,TypedProvider> providers)
            {
                foreach (var instruction in instructions)
                {
                    providers[instruction.node] = instruction.provider;
                }
            }

            public void Serialize (Dictionary<string,TypedProvider> providers)
            {
                instructions.Clear();
                if (providers == null)
                {
                    return;
                }

                foreach(var kvp in providers)
                {
                    instructions.Add(new Instruction(kvp.Key,kvp.Value));
                }
            }
        }

        [System.Serializable]
        private struct TypedProvider
        {
            [SerializeField] private Type _type;
            [SerializeField] private AvatarHintProvider _provider;

            public Type type { get { return _type; } }
            public AvatarHintProvider provider { get { return _provider; } }

            public TypedProvider(Type type, AvatarHintProvider provider)
            {
                this._type = type;
                this._provider = provider;
            }
        }

        [SerializeField]
        private SerializedAvatarHints serializedProviders;

        public enum Type
        {
            Vector3,
            Quaternion,
            Float,
            String
        }

        private Dictionary<string,TypedProvider> providers;

        public void SetProvider (string node, Type type, AvatarHintProvider provider)
        {
            Deserialize();
            providers[node] = new TypedProvider(type,provider);
            Serialize();
        }

        public bool TryGetProvider (string node, out Type type, out AvatarHintProvider provider)
        {
            Deserialize();
            if(providers.TryGetValue(node,out var typedProvider))
            {
                type = typedProvider.type;
                provider = typedProvider.provider;
                return true;
            }
            type = Type.Float;
            provider = null;
            return false;
        }

        public bool TryGetVector3 (string node, out Vector3 result)
        {
            Deserialize();
            if (TryGetProvider(node, out var type, out var provider))
            {
                if (type == Type.Vector3)
                {
                    result = provider.ProvideVector3();
                    return true;
                }
            }
            result = Vector3.zero;
            return false;
        }

        public bool TryGetQuaternion (string node, out Quaternion result)
        {
            Deserialize();
            if (TryGetProvider(node, out var type, out var provider))
            {
                if (type == Type.Quaternion)
                {
                    result = provider.ProvideQuaternion();
                    return true;
                }
            }
            result = Quaternion.identity;
            return false;
        }

        public bool TryGetFloat (string node, out float result)
        {
            Deserialize();
            if (TryGetProvider(node, out var type, out var provider))
            {
                if (type == Type.Float)
                {
                    result = provider.ProvideFloat();
                    return true;
                }
            }
            result = 0.0f;
            return false;
        }

        public bool TryGetString (string node, out string result)
        {
            Deserialize();
            if (TryGetProvider(node, out var type, out var provider))
            {
                if (type == Type.String)
                {
                    result = provider.ProvideString();
                    return true;
                }
            }
            result = string.Empty;
            return false;
        }

        private void Deserialize ()
        {
            if (providers == null)
            {
                providers = new Dictionary<string, TypedProvider>();
                serializedProviders.Deserialize(providers);
            }
        }

        private void Serialize ()
        {
#if UNITY_EDITOR
            serializedProviders.Serialize(providers);
#endif
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(AvatarHints))]
    public class AvatarHints2PropertyDrawer : PropertyDrawer
    {
        private ReorderableList _list;

        private ReorderableList GetList(SerializedProperty property)
        {
            if (_list != null)
            {
                return _list;
            }

            var serialized = property.FindPropertyRelative("serializedProviders");
            var instructions = serialized.FindPropertyRelative("instructions");

            _list = new ReorderableList(serialized.serializedObject,instructions,true,true,true,true);
            _list.drawHeaderCallback = (Rect r) =>
            {
                EditorGUI.LabelField(r, property.displayName);
            };
            _list.drawElementCallback = (Rect r, int i, bool isActive, bool isFocused) =>
            {
                var e = _list.serializedProperty.GetArrayElementAtIndex(i);
                var node = e.FindPropertyRelative("_node");
                var typedProvider = e.FindPropertyRelative("_provider");
                var type = typedProvider.FindPropertyRelative("_type");
                var provider = typedProvider.FindPropertyRelative("_provider");

                EditorGUI.PropertyField(
                    new Rect(r.x, r.y, r.width/3, EditorGUIUtility.singleLineHeight),
                    node,GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(r.x + r.width/3, r.y, r.width/3, EditorGUIUtility.singleLineHeight),
                    type, GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(r.x + (r.width*2)/3, r.y, r.width/3, EditorGUIUtility.singleLineHeight),
                    provider, GUIContent.none);
            };

            return _list;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) + GetList(property).GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var prevEnabled = GUI.enabled;
            GUI.enabled = !Application.isPlaying;

            property.serializedObject.Update();
            GetList(property).DoList(position);

            if (EditorGUI.EndChangeCheck()) {
                property.serializedObject.ApplyModifiedProperties();
            }

            GUI.enabled = prevEnabled;
        }
    }
#endif
}