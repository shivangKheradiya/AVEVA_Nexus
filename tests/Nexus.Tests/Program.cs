using System;
using System.Linq;
using System.Reflection;

namespace Nexus.Tests
{
    // Test classes are static classes named "*Tests" with a public static RunAll() that calls Program.Run(name, body).
    public static class Program
    {
        static int _passed;

        public static void Run(string name, Action test)
        {
            int before = Check.Failures;
            try { test(); }
            catch (Exception ex) { Check.Fail(name, "unexpected " + ex.GetType().Name + ": " + ex.Message); }
            if (Check.Failures == before) { _passed++; Console.WriteLine("  ok    " + name); }
        }

        public static int Main()
        {
            var suites = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass && t.IsAbstract && t.IsSealed && t.Name.EndsWith("Tests"));
            foreach (var t in suites)
                t.GetMethod("RunAll", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);

            Console.WriteLine(_passed + " passed, " + Check.Failures + " failed");
            return Check.Failures == 0 ? 0 : 1;
        }
    }

    public static class RunnerSelfTests
    {
        public static void RunAll()
        {
            Program.Run("runner works", () => Check.Equal(1, 1, "one"));
        }
    }
}
