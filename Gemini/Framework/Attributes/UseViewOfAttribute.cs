using System;

namespace Gemini.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UseViewOfAttribute : Attribute
    {
        public Type SelectedType { get; set; }
        public UseViewOfAttribute(Type type) => SelectedType = type;
    }
}
