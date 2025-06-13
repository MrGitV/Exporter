using System.Collections.Generic;
using System.Threading.Tasks;

namespace Exporter
{
    // Extension method to convert IEnumerable<T> to IAsyncEnumerable<T> for asynchronous streaming
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.Yield();
            }
        }
    }
}
