public static class LinqExt
{
    public static void Let<T>(this T self, System.Action<T> act) { if (self != null) act(self); }
}
