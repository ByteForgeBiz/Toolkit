using ByteForge.Toolkit.CommandLine;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Text;

namespace ByteForge.Toolkit.CLI;
/// <summary>
/// Provides functionality for parsing command line arguments using <see cref="System.CommandLine"/>.
/// Supports token replacement for argument preprocessing.
/// </summary>
public class CommandParser
{
    private readonly System.CommandLine.Parsing.Parser _parser;
    private readonly IDictionary<string, string> _tokenReplacer;
    private readonly RootCommandBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandParser"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="RootCommandBuilder"/> containing configuration settings.</param>
    /// <param name="parser">The <see cref="System.CommandLine.Parsing.Parser"/> to use for parsing arguments.</param>
    /// <param name="tokenReplacer">
    /// An optional dictionary of token replacements. If provided, arguments matching a key will be replaced with the corresponding value before parsing.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="parser"/> is <c>null</c>.</exception>
    internal CommandParser(RootCommandBuilder builder,
                           System.CommandLine.Parsing.Parser parser,
                           IDictionary<string, string>? tokenReplacer = null)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _tokenReplacer = tokenReplacer ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Gets the <see cref="CommandLineConfiguration"/> associated with the parser.
    /// </summary>
    public CommandLineConfiguration Configuration => _parser.Configuration;

    /// <summary>
    /// Parses the specified command line arguments, applying token replacement if configured.
    /// Displays banner and parameter explanation if enabled.
    /// </summary>
    /// <param name="args">The command line arguments to parse.</param>
    /// <returns>
    /// A <see cref="ParseResult"/> representing the result of parsing the arguments.
    /// </returns>
    public ParseResult Parse(params string[] args)
    {
        // Display banner if configured
        _builder.BannerAction?.Invoke();

        // Replace tokens in arguments if any replacements are configured
        var caseCorrectedArgs = new List<string>(args.Length);
        foreach (var arg in args)
            if (_tokenReplacer.TryGetValue(arg, out var replacement))
                caseCorrectedArgs.Add(replacement);
            else
                caseCorrectedArgs.Add(arg);

        // Process global options first and filter them out
        var filteredArgs = ProcessGlobalOptions(caseCorrectedArgs.ToArray());

        // Parse the (possibly modified) arguments
        var result = new ParseResult(_parser.Parse(filteredArgs));

        // Display parameter explanation if enabled and not a help command
        if (_builder.EnableParameterExplanation && !IsHelpCommand(result))
            Console.WriteLine(DescribeCommand(result));

        return result;
    }

    /// <summary>
    /// Describes the parsed command on the console, showing its name, options, and arguments.
    /// </summary>
    /// <param name="cmd">The parsed command result.</param>
    /// <returns>A string description of the parsed command.</returns>
    private string DescribeCommand(ParseResult cmd)
    {
        if (cmd == null)
            return "No command was parsed.";

        var sb = new StringBuilder();
        var descWriter = new StringWriter(sb);

        var command = cmd.CommandResult.Command;

        // Build the full command path (group hierarchy)
        var commandPath = GetCommandPath(cmd.CommandResult);
        descWriter.WriteLine($"Running    : {commandPath}");
        descWriter.WriteLine($"Description: {command.Description}");

        DescribeOptions(cmd, descWriter);
        DescribeArguments(cmd, descWriter);
        return sb.ToString();
    }

    private static void DescribeOptions(ParseResult cmd, StringWriter descWriter)
    {
        var command = cmd.CommandResult.Command;

        // Only display options that were actually passed to the command
        var passedOptions = command.Options
            .Where(opt => cmd.FindResultFor(opt) is OptionResult)
            .ToList();

        DescribeSymbols<Option>(command.Options, cmd, descWriter);
    }

    private static void DescribeArguments(ParseResult cmd, StringWriter descWriter)
    {
        var command = cmd.CommandResult.Command;

        // Only display arguments that were actually passed to the command
        var passedArguments = command.Arguments
            .Where(arg => cmd.FindResultFor(arg) is ArgumentResult)
            .ToList();

        DescribeSymbols<Argument>(command.Arguments, cmd, descWriter);
    }

    private static void DescribeSymbols<T>(IReadOnlyList<T> symbols, ParseResult cmd, StringWriter descWriter) where T : Symbol
    {
        var command = cmd.CommandResult.Command;
        // Only display symbols that were actually passed to the command
        var passedSymbols = command.Children
            // Filter to only the symbols that were passed
            .Where(sym => symbols.Contains(sym))
            // Map to an anonymous object with name, description, and value
            .Select(s =>
            {
                // Get the value assigned to the symbol, if any
                var sym = cmd.FindResultFor(s);
                if (sym == null) return null;

                // The symbol is an Argument or Option
                var val = (s is Argument arg) ? cmd.GetValueForArgument(arg)
                        : (s is Option opt) ? cmd.GetValueForOption(opt)
                        : null;
                return new
                {
                    s.Name,
                    s.Description,
                    Value = val
                };
            })
            // Only include symbols with values
            .Where(s => s != null && s.Value != null)
            .ToList();

        if (passedSymbols.Count == 0)
            return;

        var plural = passedSymbols.Count != 1 ? "s" : string.Empty;
        descWriter.WriteLine($"\n{typeof(T).Name}{plural}:");

        var len = passedSymbols.Max(a => a?.Name.Length) ?? 0;
        foreach (var symbol in passedSymbols)
        {
            descWriter.WriteLine($"  {symbol?.Name.PadRight(len)}: {symbol?.Value}");
            descWriter.WriteLine($"    > {symbol?.Description}");
        }
    }

    /// <summary>
    /// Gets the full command path including all parent commands (command group).
    /// </summary>
    /// <param name="commandResult">The command result for which to build the path.</param>
    /// <returns>A string representing the full command path.</returns>
    private string GetCommandPath(CommandResult commandResult)
    {
        var path = new List<string>();
        var current = commandResult;

        // Build the command path from the bottom up
        while (current != null)
        {
            path.Add(current.Command.Name);
            current = current.Parent as CommandResult;
        }

        // Reverse to get root-to-leaf order
        path.Reverse();

        // Join the path components with spaces to match command-line usage
        return string.Join(" ", path);
    }

    /// <summary>
    /// Determines whether the parsed command is a help request.
    /// </summary>
    /// <param name="parseResult">The parsed command result.</param>
    /// <returns>True if the command is a help request; otherwise, false.</returns>
    private bool IsHelpCommand(ParseResult parseResult)
    {
        if (parseResult == null)
            return false;

        // Check if the HelpOption is present in the parse result
        // System.CommandLine automatically adds a help option to all commands
        var helpOption = parseResult.CommandResult.Command.Options
            .FirstOrDefault(o => o.Name == "help" || o.Aliases.Contains("-h") || o.Aliases.Contains("/?"));

        if (helpOption != null && parseResult.FindResultFor(helpOption) != null)
            return true;

        // Some additional checks that might indicate a help request
        return parseResult.Errors.Any(e => e.Message.Contains("help")) ||
               parseResult.CommandResult.Command.Name.Equals("help", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Processes global options from the command line arguments and executes their actions.
    /// </summary>
    /// <param name="args">The command line arguments to process.</param>
    /// <returns>The filtered arguments with global options removed.</returns>
    private string[] ProcessGlobalOptions(string[] args)
    {
        var globalOptions = _builder.GlobalOptions;
        if (globalOptions.Count == 0)
            return args;

        var filteredArgs = new List<string>();
        var showGlobalHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var processed = false;

            // Check if this argument is a help request
            if (IsHelpArg(arg))
            {
                showGlobalHelp = true;
                filteredArgs.Add(arg);
                continue;
            }

            // Check if this argument matches any global option
            foreach (var globalOption in globalOptions)
            {
                if (globalOption.Matches(arg))
                {
                    processed = true;

                    if (globalOption.ExpectsValue)
                    {
                        // Get the next argument as the value
                        if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                        {
                            i++; // Skip the value argument
                            globalOption.Action(args[i]);
                        }
                        else
                        {
                            globalOption.Action(string.Empty); // No value provided
                        }
                    }
                    else
                    {
                        globalOption.Action(string.Empty);
                    }
                    break;
                }
            }

            if (!processed)
            {
                filteredArgs.Add(arg);
            }
        }

        // Show global options help if help was requested and we have global options
        if (showGlobalHelp)
        {
            ShowGlobalOptionsHelp();
        }

        return filteredArgs.ToArray();
    }

    /// <summary>
    /// Determines if an argument is a help request.
    /// </summary>
    /// <param name="arg">The argument to check.</param>
    /// <returns>True if the argument is a help request; otherwise, false.</returns>
    private static bool IsHelpArg(string arg)
    {
        return arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("-?", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("/?", StringComparison.OrdinalIgnoreCase) ||
               arg.Equals("help", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Shows help information about global options.
    /// </summary>
    private void ShowGlobalOptionsHelp()
    {
        var globalOptions = _builder.GlobalOptions;
        if (globalOptions.Count == 0)
            return;

        Console.WriteLine();
        Console.WriteLine("Global Options:");

        foreach (var option in globalOptions)
        {
            var aliases = option.AllAliases?.Take(3).ToArray() ?? [];
            var aliasText = aliases.Length > 0 ? string.Join(", ", aliases) : $"--{option.Name}";

            if (option.ExpectsValue)
            {
                Console.WriteLine($"  {aliasText} <value>    {option.Description}");
            }
            else
            {
                Console.WriteLine($"  {aliasText}    {option.Description}");
            }
        }
        Console.WriteLine();
    }
}
