using UnityEngine;

namespace Data
{
    public class EditorButtonAttribute : PropertyAttribute
    {
        public readonly string Method;
        public readonly string Label;

        public EditorButtonAttribute(string method, string label = null)
        {
            Method = method;
            Label = label;
        }
    }
}