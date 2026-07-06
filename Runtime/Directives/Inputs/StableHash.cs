namespace Armoury.UI
{
    public class StableHash
    {
        public static ulong Fnv1A64(string text)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            var hash = offset;

            for (var i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= prime;
            }

            return hash;
        }
    }
}