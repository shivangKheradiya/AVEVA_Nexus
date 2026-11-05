using System;
using AVEVA_Nexus.Exceptions;
using AVEVA_Nexus.Validation;

namespace Nexus.Tests
{
    public static class PmlInputValidatorTests
    {
        static void Rejects(Func<string> call, string name)
        {
            var ex = Check.Throws<NexusException>(() => call(), name);
            if (ex != null) Check.Equal(400, ex.StatusCode, name + " status");
        }

        public static void RunAll()
        {
            Program.Run("Type_accepts_PIPE_and_uppercases", () =>
            {
                Check.Equal("PIPE", PmlInputValidator.Type("pipe"), "lower");
                Check.Equal("PIPE", PmlInputValidator.Type(" PIPE "), "trim");
            });

            Program.Run("Type_rejects_space_semicolon_and_empty", () =>
            {
                foreach (var bad in new[] { "PIPE;x", "A B", "", "   ", null, new string('A', 33) })
                    Rejects(() => PmlInputValidator.Type(bad), "Type(" + bad + ")");
            });

            Program.Run("Attribute_accepts_system_and_UDA_names", () =>
            {
                Check.Equal("NAME", PmlInputValidator.Attribute("name"), "name");
                Check.Equal(":TPSPEC", PmlInputValidator.Attribute(":TPSPEC"), "uda");
                Check.Equal(":TP-MTO-AREA", PmlInputValidator.Attribute(":tp-mto-area"), "uda with hyphens");
            });

            Program.Run("Attribute_rejects_bad_characters", () =>
            {
                foreach (var bad in new[] { "NAME;", "A B", "", null, "N|AME", new string('A', 65) })
                    Rejects(() => PmlInputValidator.Attribute(bad), "Attribute(" + bad + ")");
            });

            Program.Run("VariableName_accepts_plain_names", () =>
            {
                Check.Equal("nexusTexto", PmlInputValidator.VariableName("nexusTexto"), "plain");
                Check.Equal("my_var.1", PmlInputValidator.VariableName("my_var.1"), "underscore and dot");
            });

            Program.Run("VariableName_rejects_injection", () =>
            {
                foreach (var bad in new[] { "x ) ; x", "x;y", "", null, "a b", "x\ny", "$M/x", new string('a', 65) })
                    Rejects(() => PmlInputValidator.VariableName(bad), "VariableName(" + bad + ")");
            });

            Program.Run("DbRef_accepts_valid_ref", () =>
                Check.Equal("=201346360/1", PmlInputValidator.DbRef("=201346360/1"), "ref"));

            Program.Run("DbRef_rejects_garbage", () =>
            {
                foreach (var bad in new[] { "garbage", "=1", "=1/2;x", "", null, "/SITE", "=a/b" })
                    Rejects(() => PmlInputValidator.DbRef(bad), "DbRef(" + bad + ")");
            });

            Program.Run("Scope_null_or_blank_is_null", () =>
            {
                Check.True(PmlInputValidator.Scope(null) == null, "null");
                Check.True(PmlInputValidator.Scope("  ") == null, "blank");
            });

            Program.Run("Scope_accepts_name_and_dbref", () =>
            {
                Check.Equal("/CAR-480-PI", PmlInputValidator.Scope("/CAR-480-PI"), "name");
                Check.Equal("=1/2", PmlInputValidator.Scope("=1/2"), "dbref");
            });

            Program.Run("Scope_rejects_injection", () =>
            {
                foreach (var bad in new[] { "/A;B", "/A B", "A", "$M/x", "/A\nB" })
                    Rejects(() => PmlInputValidator.Scope(bad), "Scope(" + bad + ")");
            });

            Program.Run("Where_null_or_blank_is_null", () =>
            {
                Check.True(PmlInputValidator.Where(null) == null, "null");
                Check.True(PmlInputValidator.Where("") == null, "empty");
            });

            Program.Run("Where_accepts_normal_conditions", () =>
            {
                const string w = "(:TPSPEC EQ 'A') AND (BORE GT 50)";
                Check.Equal(w, PmlInputValidator.Where(w), "condition");
            });

            Program.Run("Where_rejects_each_forbidden_construct", () =>
            {
                foreach (var bad in new[] { "a; b", "a\nb", "a\rb", "$M/x", "a`b", "SAVEWORK", "delete all", "x EQ 1 AND NEW PIPE", "(unbalanced", "unbalanced)", "x)(y", "remove x", "undo", "store x", "macro y", "finish", "quit", "onerror continue", "handle any" })
                    Rejects(() => PmlInputValidator.Where(bad), "Where(" + bad + ")");
            });

            Program.Run("Where_does_not_reject_substrings_of_forbidden_words", () =>
            {
                Check.Equal("RENEWAL EQ 1", PmlInputValidator.Where("RENEWAL EQ 1"), "RENEWAL contains NEW");
                Check.Equal("HANDLER EQ 2", PmlInputValidator.Where("HANDLER EQ 2"), "HANDLER contains HANDLE");
            });

            Program.Run("Where_rejects_parenthesis_breakout_hidden_in_strings", () =>
            {
                // Review finding: parentheses inside string literals were counted, so this balanced and passed,
                // while PML closes the with(...) group at the first ')' after the string.
                foreach (var bad in new[] { "NAME EQ |(|) DESC |x|", "NAME EQ '(') DESC 'x'", "NAME EQ |)| ) (" })
                    Rejects(() => PmlInputValidator.Where(bad), "Where(" + bad + ")");
            });

            Program.Run("Where_ignores_parentheses_inside_strings", () =>
            {
                Check.Equal("NAME EQ |(x)|", PmlInputValidator.Where("NAME EQ |(x)|"), "pipes");
                Check.Equal("NAME EQ '))' OR NAME EQ '(('", PmlInputValidator.Where("NAME EQ '))' OR NAME EQ '(('"), "quotes");
            });

            Program.Run("Where_rejects_unterminated_strings", () =>
            {
                foreach (var bad in new[] { "NAME EQ |abc", "NAME EQ 'abc", "(NAME EQ |a|) AND 'b" })
                    Rejects(() => PmlInputValidator.Where(bad), "Where(" + bad + ")");
            });

            Program.Run("Where_rejects_pml_variables_objects_and_method_calls", () =>
            {
                foreach (var bad in new[] { "!!x EQ 1", "NAME EQ !a", "OBJECT FILE(|x|) EQ 1", "NAME.size() GT 1", "x .copy ( 1 )" })
                    Rejects(() => PmlInputValidator.Where(bad), "Where(" + bad + ")");
            });

            Program.Run("Where_rejects_more_modifying_words", () =>
            {
                foreach (var bad in new[] { "RENAME x", "copy x", "MOVE y", "include z", "reorder", "claim", "unclaim" })
                    Rejects(() => PmlInputValidator.Where(bad), "Where(" + bad + ")");
            });

            Program.Run("Where_allows_forbidden_words_inside_strings_and_attribute_names", () =>
            {
                Check.Equal("NAME EQ |/NEW-TANK|", PmlInputValidator.Where("NAME EQ |/NEW-TANK|"), "word in string");
                Check.Equal("NAME EQ '/DELETE ME'", PmlInputValidator.Where("NAME EQ '/DELETE ME'"), "word in quoted string");
                Check.Equal(":NEW-FLAG EQ 1", PmlInputValidator.Where(":NEW-FLAG EQ 1"), "word inside a UDA name");
            });

            Program.Run("Where_rejects_over_500_chars", () =>
                Rejects(() => PmlInputValidator.Where("(" + new string('A', 500) + ")"), "too long"));
        }
    }
}
