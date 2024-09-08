using System;

namespace Gemini.Framework.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class UseViewAttribute : Attribute
    {
        public object Context { get; set; }

        public Type ViewType { get; }

        public UseViewAttribute(Type viewType)
        {
            ViewType = viewType;
        }
    }
}
