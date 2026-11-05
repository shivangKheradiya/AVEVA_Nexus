using System;
using AVEVA_Nexus.Config;

namespace Nexus.Tests
{
    public static class ReadOnlyModeTests
    {
        static void WithEnv(string value, Action body)
        {
            string old = Environment.GetEnvironmentVariable("NEXUS_READONLY");
            try
            {
                Environment.SetEnvironmentVariable("NEXUS_READONLY", value);
                body();
            }
            finally
            {
                Environment.SetEnvironmentVariable("NEXUS_READONLY", old);
            }
        }

        public static void RunAll()
        {
            Program.Run("ReadOnlyMode_is_on_when_variable_is_not_set", () =>
                WithEnv(null, () => Check.True(ReadOnlyMode.IsOn, "unset")));

            Program.Run("ReadOnlyMode_is_off_only_with_exactly_0", () =>
                WithEnv("0", () => Check.True(!ReadOnlyMode.IsOn, "0")));

            Program.Run("ReadOnlyMode_stays_on_for_any_other_value", () =>
            {
                foreach (var v in new[] { "1", "", "false", "no", "00", " 0" })
                    WithEnv(v, () => Check.True(ReadOnlyMode.IsOn, "value '" + v + "'"));
            });
        }
    }
}
