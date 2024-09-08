using System.Reflection;

namespace MarioIDE.Mupen;

internal static class ReflectionHelper
{
    public static Dictionary<string, TAttribute> GetPropertyAttributeDictionaryOfType<TAttribute>(this Type type)
        where TAttribute : Attribute
    {
        Dictionary<string, TAttribute> attr = (from prop in type.GetProperties()
                from attribs in prop.GetCustomAttributes(typeof(TAttribute)).Cast<TAttribute>()
                select new KeyValuePair<string, TAttribute>(prop.Name, attribs))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return attr;
    }
}