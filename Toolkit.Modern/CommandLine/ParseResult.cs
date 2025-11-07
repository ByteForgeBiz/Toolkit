using System.CommandLine;
using System.CommandLine.Parsing;

namespace ByteForge.Toolkit.CommandLine;
/// <summary>
/// Represents the result of parsing command line arguments.
/// Wraps <see cref="System.CommandLine.Parsing.ParseResult"/> and exposes its functionality.
/// </summary>
public class ParseResult
{
    private readonly System.CommandLine.Parsing.ParseResult _inner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParseResult"/> class.
    /// </summary>
    /// <param name="parseResult">The inner <see cref="System.CommandLine.Parsing.ParseResult"/> to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="parseResult"/> is <see langword="null"/>.</exception>
    internal ParseResult(System.CommandLine.Parsing.ParseResult parseResult)
    {
        _inner = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
    }

    /// <summary>
    /// Invokes the parsed command and returns the exit code.
    /// </summary>
    /// <returns>The exit code of the command execution.</returns>
    public int Invoke() => _inner.Invoke();

    /// <summary>
    /// Gets the <see cref="CommandResult"/> for the parsed command.
    /// </summary>
    public CommandResult CommandResult => _inner.CommandResult;

    /// <summary>
    /// Gets the list of errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<ParseError> Errors => _inner.Errors;

    /// <summary>
    /// Finds the result for the specified symbol.
    /// </summary>
    /// <param name="symbol">The symbol to find the result for.</param>
    /// <returns>The result for the symbol, or <see langword="null"/> if not found.</returns>
    public SymbolResult? FindResultFor(Symbol symbol) => _inner.FindResultFor(symbol);

    /// <summary>
    /// Gets the value for the specified option.
    /// </summary>
    /// <typeparam name="T">The type of the option value.</typeparam>
    /// <param name="option">The option to get the value for.</param>
    /// <returns>The value for the option.</returns>
    public T? GetValueForOption<T>(Option<T> option) => _inner.GetValueForOption(option);

    /// <summary>
    /// Gets the value for the specified option.
    /// </summary>
    /// <param name="option">The option to get the value for.</param>
    /// <returns>The value for the option.</returns>
    public object? GetValueForOption(Option option) => _inner.GetValueForOption(option);

    /// <summary>
    /// Gets the value for the specified argument.
    /// </summary>
    /// <typeparam name="T">The type of the argument value.</typeparam>
    /// <param name="argument">The argument to get the value for.</param>
    /// <returns>The value for the argument.</returns>
    public T GetValueForArgument<T>(Argument<T> argument) => _inner.GetValueForArgument(argument);

    /// <summary>
    /// Gets the value for the specified argument.
    /// </summary>
    /// <param name="argument">The argument to get the value for.</param>
    /// <returns>The value for the argument.</returns>
    public object? GetValueForArgument(Argument argument) => _inner.GetValueForArgument(argument);
}
