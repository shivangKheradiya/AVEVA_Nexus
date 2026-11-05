using System.Collections.Generic;

namespace AVEVA_Nexus.Query
{
    public static class Paging
    {
        public const int DefaultLimit = 100;
        public const int MaxLimit = 1000;

        public static (int offset, int limit) Normalize(int? offset, int? limit)
        {
            int o = offset.HasValue && offset.Value > 0 ? offset.Value : 0;
            int l = limit ?? DefaultLimit;
            if (l < 1) l = 1;
            if (l > MaxLimit) l = MaxLimit;
            return (o, l);
        }

        public static List<T> Slice<T>(IList<T> all, int offset, int limit)
        {
            var page = new List<T>();
            for (int i = offset; i < all.Count && page.Count < limit; i++) page.Add(all[i]);
            return page;
        }
    }
}
