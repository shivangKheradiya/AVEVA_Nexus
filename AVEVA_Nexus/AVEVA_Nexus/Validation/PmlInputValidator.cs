using AVEVA_Nexus.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace AVEVA_Nexus.Validation
{
    // Validates user input before it is placed into a PML command line. Throws NexusException (400) on bad input.
    // Pure logic: must not reference Aveva.* (it is compiled by the unit-test runner).
    public static class PmlInputValidator
    {
        static readonly Regex TypeRx = new Regex("^[A-Za-z0-9_]{1,32}$");
        static readonly Regex AttributeRx = new Regex("^[A-Za-z0-9_:\\-]{1,64}$"); // '-' is needed for UDAs such as :TP-MTO-AREA
        static readonly Regex VariableNameRx = new Regex("^[A-Za-z0-9_.]{1,64}$");
        static readonly Regex DbRefRx = new Regex("^=[0-9]+/[0-9]+$");
        static readonly Regex NameRx = new Regex("^/[A-Za-z0-9_\\-/.]{1,200}$");

        static readonly string[] Forbidden =
        {
            "SAVEWORK", "DELETE", "REMOVE", "NEW", "UNDO", "STORE", "MACRO", "FINISH", "QUIT", "ONERROR", "HANDLE",
            "RENAME", "COPY", "MOVE", "INCLUDE", "REORDER", "CLAIM", "UNCLAIM", "OBJECT"
        };
        // Whole words only, and not when glued to an attribute/UDA name character: RENEWAL, HANDLER, :NEW-FLAG are fine.
        static readonly Regex ForbiddenRx = new Regex(
            "(?<![A-Za-z0-9_:\\-])(" + string.Join("|", Forbidden) + ")(?![A-Za-z0-9_\\-])", RegexOptions.IgnoreCase);
        // x.method(...) : a call on an object, which can have side effects
        static readonly Regex MethodCallRx = new Regex("\\.\\s*[A-Za-z_][A-Za-z0-9_]*\\s*\\(");

        const int MaxWhereLength = 500;

        static NexusException Bad(string what) { return new NexusException("Invalid " + what, 400); }

        public static string Type(string value)
        {
            string v = (value ?? "").Trim();
            if (!TypeRx.IsMatch(v)) throw Bad("element type");
            return v.ToUpperInvariant();
        }

        public static string Attribute(string value)
        {
            string v = (value ?? "").Trim();
            if (!AttributeRx.IsMatch(v)) throw Bad("attribute name");
            return v.ToUpperInvariant();
        }

        // Name of a global PML variable (without "!!"); it is interpolated into PML commands.
        public static string VariableName(string value)
        {
            string v = (value ?? "").Trim();
            if (!VariableNameRx.IsMatch(v)) throw Bad("variable name");
            return v;
        }

        public static string DbRef(string value)
        {
            string v = (value ?? "").Trim();
            if (!DbRefRx.IsMatch(v)) throw Bad("dbref (expected =<number>/<number>)");
            return v;
        }

        public static string Scope(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string v = value.Trim();
            if (DbRefRx.IsMatch(v) || NameRx.IsMatch(v)) return v;
            throw Bad("scope (expected a dbref or a name starting with '/')");
        }

        public static string Where(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string v = value.Trim();

            if (v.Length > MaxWhereLength) throw Bad("where condition (too long)");
            if (v.IndexOfAny(new[] { ';', '\n', '\r', '$', '`', '!' }) >= 0) throw Bad("where condition (forbidden character)");

            // Scan like PML does: the contents of '...' and |...| strings are data, so parentheses and words
            // inside them must not count. Everything outside strings is "code" and is checked.
            var code = new System.Text.StringBuilder();
            char quote = '\0';
            foreach (char c in v)
            {
                if (quote != '\0')
                {
                    if (c == quote) quote = '\0';
                    code.Append(' ');
                }
                else if (c == '\'' || c == '|')
                {
                    quote = c;
                    code.Append(' ');
                }
                else
                {
                    code.Append(c);
                }
            }
            if (quote != '\0') throw Bad("where condition (unterminated string)");

            string outside = code.ToString();
            if (ForbiddenRx.IsMatch(outside)) throw Bad("where condition (forbidden word)");
            if (MethodCallRx.IsMatch(outside)) throw Bad("where condition (method calls are not allowed)");

            int depth = 0;
            foreach (char c in outside)
            {
                if (c == '(') depth++;
                else if (c == ')' && --depth < 0) break;
            }
            if (depth != 0) throw Bad("where condition (unbalanced parentheses)");
            return v;
        }
    }
}
