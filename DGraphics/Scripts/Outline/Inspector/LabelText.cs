using UnityEditor;
using UnityEngine;

namespace DGraphics.Inspector
{
    public class LabelTextAttribute : PropertyAttribute
    {
        public string Label { get; private set; }

        public LabelTextAttribute(string label)
        {
            Label = label;
        }
    }
}

