
namespace twitterXcrypto.util
{
    internal static class EnumerableExtensions
    {
        internal static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        internal static bool Empty<T>(this IEnumerable<T> enumerable) => !enumerable.Any();

        internal static bool ContainsEvery<T>(this IEnumerable<T> enumerable, IEnumerable<T> matches)
        {
            foreach (var match in matches)
            {
                if (!enumerable.Contains(match))
                    return false;
            }
            return true;
        } 
    }
}
