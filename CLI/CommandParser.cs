using System;
using System.Collections.Generic;
using System.CommandLine;

namespace ByteForge.Toolkit.CLI
{
    /// <summary>
    /// Provides functionality for parsing command line arguments using <see cref="System.CommandLine"/>.
    /// Supports token replacement for argument preprocessing.
    /// </summary>
    public class CommandParser
    {
        private readonly System.CommandLine.Parsing.Parser _parser;
        private readonly IDictionary<string, string> _tokenReplacer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandParser"/> class.
        /// </summary>
        /// <param name="parser">The <see cref="System.CommandLine.Parsing.Parser"/> to use for parsing arguments.</param>
        /// <param name="tokenReplacer">
        /// An optional dictionary of token replacements. If provided, arguments matching a key will be replaced with the corresponding value before parsing.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="parser"/> is <c>null</c>.</exception>
        internal CommandParser(System.CommandLine.Parsing.Parser parser, IDictionary<string, string> tokenReplacer = null)
        {
            if (parser is null)
                throw new ArgumentNullException(nameof(parser));
            _parser = parser;
            _tokenReplacer = tokenReplacer ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the <see cref="CommandLineConfiguration"/> associated with the parser.
        /// </summary>
        public CommandLineConfiguration Configuration => _parser.Configuration;

        /// <summary>
        /// Parses the specified command line arguments, applying token replacement if configured.
        /// </summary>
        /// <param name="args">The command line arguments to parse.</param>
        /// <returns>
        /// A <see cref="System.CommandLine.Parsing.ParseResult"/> representing the result of parsing the arguments.
        /// </returns>
        public System.CommandLine.Parsing.ParseResult Parse(params string[] args)
        {
            var replacedArgs = new List<string>(args.Length);
            foreach (var arg in args)
                if (_tokenReplacer.TryGetValue(arg, out var replacement))
                    replacedArgs.Add(replacement);
                else
                    replacedArgs.Add(arg);

            return _parser.Parse(replacedArgs);
        }

    }
}
