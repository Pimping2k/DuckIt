using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DefaultNamespace
{
    public interface IIdOwner
    {
        ushort Id { get; }
    }

    public abstract class IdConfig : ScriptableObject, IIdOwner
    {
        [SerializeField, Unity.Collections.ReadOnly] private ushort _id;

        public ushort Id => _id;

#if UNITY_EDITOR
        public void SetId(ushort id)
        {
            _id = id;
            EditorUtility.SetDirty(this);
        }
#endif
    }
}