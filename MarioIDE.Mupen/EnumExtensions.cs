namespace MarioIDE.Mupen;

internal static class EnumExtensions
{
    public static IEnumerable<T> EnumToArray<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }
}