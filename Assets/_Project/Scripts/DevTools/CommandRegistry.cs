using System;
using System.Collections.Generic;

namespace Game.DevTools
{
    /// <summary>
    /// Text commands for the dev console ("spawn vase 3", "timescale 0.5"). Pure, so the parsing is
    /// testable; the on-screen console only has to feed it lines and print what comes back.
    /// </summary>
    public sealed class CommandRegistry
    {
        public delegate string Handler(string[] args);

        private readonly Dictionary<string, Handler> _commands =
            new Dictionary<string, Handler>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _help =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void Register(string name, Handler handler, string help = "")
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Command needs a name.", nameof(name));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            _commands[name] = handler;
            _help[name] = help ?? "";
        }

        public bool Has(string name) => _commands.ContainsKey(name);

        public IEnumerable<string> Names
        {
            get
            {
                var names = new List<string>(_commands.Keys);
                names.Sort(StringComparer.OrdinalIgnoreCase);
                return names;
            }
        }

        public string HelpFor(string name) => _help.TryGetValue(name, out var h) ? h : "";

        /// <summary>Runs one line. Returns the text to show the user (never throws for bad input).</summary>
        public string Execute(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return "";

            var tokens = Tokenise(line);
            if (tokens.Count == 0) return "";

            string name = tokens[0];
            if (!_commands.TryGetValue(name, out var handler))
                return $"Unknown command '{name}'. Type 'help'.";

            var args = tokens.GetRange(1, tokens.Count - 1).ToArray();
            try
            {
                return handler(args) ?? "";
            }
            catch (Exception e)
            {
                return $"Error: {e.Message}";
            }
        }

        /// <summary>Splits on whitespace, keeping "quoted phrases" together.</summary>
        public static List<string> Tokenise(string line)
        {
            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0) tokens.Add(current.ToString());
            return tokens;
        }
    }
}
