using System.Collections.Generic;
using AVEVA_Nexus.Query;

namespace Nexus.Tests
{
    public static class PagingTests
    {
        static List<int> Numbers(int n)
        {
            var l = new List<int>();
            for (int i = 1; i <= n; i++) l.Add(i);
            return l;
        }

        public static void RunAll()
        {
            Program.Run("Normalize_applies_defaults", () =>
            {
                var n = Paging.Normalize(null, null);
                Check.Equal(0, n.offset, "offset");
                Check.Equal(100, n.limit, "limit");
            });

            Program.Run("Normalize_clamps_limit_and_offset", () =>
            {
                Check.Equal(1000, Paging.Normalize(null, 5000).limit, "limit above max");
                Check.Equal(1, Paging.Normalize(null, 0).limit, "limit zero");
                Check.Equal(1, Paging.Normalize(null, -3).limit, "limit negative");
                Check.Equal(0, Paging.Normalize(-5, null).offset, "offset negative");
                Check.Equal(40, Paging.Normalize(40, 10).offset, "offset kept");
                Check.Equal(10, Paging.Normalize(40, 10).limit, "limit kept");
            });

            Program.Run("Slice_returns_the_requested_window", () =>
            {
                var s = Paging.Slice(Numbers(10), 2, 3);
                Check.Equal(3, s.Count, "count");
                Check.Equal(3, s[0], "first");
                Check.Equal(5, s[2], "last");
            });

            Program.Run("Slice_offset_beyond_total_returns_empty", () =>
            {
                Check.Equal(0, Paging.Slice(Numbers(10), 100000, 5).Count, "far beyond");
                Check.Equal(0, Paging.Slice(Numbers(10), 10, 5).Count, "exactly at total");
                Check.Equal(0, Paging.Slice(Numbers(0), 0, 5).Count, "empty source");
            });

            Program.Run("Slice_limit_beyond_end_is_truncated", () =>
            {
                var s = Paging.Slice(Numbers(10), 8, 50);
                Check.Equal(2, s.Count, "count");
                Check.Equal(10, s[1], "last");
            });
        }
    }
}
