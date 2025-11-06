using System;
using System.Threading;

namespace ByteForge.Toolkit;
/// <summary>
/// Provides a console-based spinner animation at a fixed or dynamic position.
/// Useful for indicating progress in console applications.
/// </summary>
/// <remarks>
/// Thread-safe for concurrent calls to <see cref="Start"/>, <see cref="Stop"/>, and property setters.
/// If Unicode is not supported by the current console (e.g., on some Windows environments or when output encoding is not UTF-8),
/// the spinner gracefully falls back to an ASCII style regardless of the requested spinner style.
/// Spinner character selection is handled at construction time via the <c>GetSpinnerChars</c> method.
/// </remarks>
public class ConsoleSpinner : IDisposable
{
    #region Fields

    private static readonly object _globalConsoleLock = new object();
    private readonly string _spinChars;
    private readonly Thread? _spinThread;
    private volatile bool _isSpinning;
    private readonly object _lockObject = new object();
    private ConsoleColor? _color;
    private string _message;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the foreground color used while rendering the spinner (including the optional message).
    /// </summary>
    /// <remarks>
    /// If not explicitly set, the color at construction time (current <see cref="Console.ForegroundColor"/>) is used.
    /// Setting this property after the spinner has started will affect subsequent frames.
    /// </remarks>
    public ConsoleColor? Color
    {
        get => _color;
        set
        {
            if (_color == value) return;
            lock (_lockObject) _color = value;
        }
    }

    /// <summary>
    /// Gets the X (column) position where the spinner is rendered.
    /// A value of -1 indicates the current cursor column is used dynamically for every frame.
    /// </summary>
    public int PositionX { get; }

    /// <summary>
    /// Gets the Y (row) position where the spinner is rendered.
    /// A value of -1 indicates the current cursor row is used dynamically for every frame.
    /// </summary>
    public int PositionY { get; }

    /// <summary>
    /// Gets the delay in milliseconds between spinner frame updates.
    /// </summary>
    public int Delay { get; }

    /// <summary>
    /// Gets a value indicating whether the spinner thread is currently marked as running.
    /// </summary>
    public bool IsRunning
    {
        get { lock (_lockObject) return _isSpinning; }
    }

    /// <summary>
    /// Gets or sets the message displayed after the spinner character (if any).
    /// </summary>
    /// <remarks>
    /// Setting this property while the spinner is active updates the message on the next frame.
    /// </remarks>
    public string Message
    {
        get { lock (_lockObject) return _message; }
        set { lock (_lockObject) _message = value; }
    }

    /// <summary>
    /// Determines whether Unicode spinner styles can be rendered in the current console.
    /// </summary>
    private static bool IsUnicodeSupported => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || Console.OutputEncoding.WebName == "utf-8";

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a spinner using default values (dynamic cursor position, default style, default delay).
    /// </summary>
    public ConsoleSpinner() : this(-1, -1, null, null, null, null) { }

    /// <summary>
    /// Initializes a spinner with a message.
    /// </summary>
    /// <param name="message">The message to display next to the spinner.</param>
    public ConsoleSpinner(string message) : this(-1, -1, message, null, null, null) { }

    /// <summary>
    /// Initializes a spinner with a message and predefined style.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="style">The spinner style.</param>
    public ConsoleSpinner(string message, SpinnerStyle style) : this(-1, -1, message, null, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with a message, style, and frame delay.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="style">The spinner style.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string message, SpinnerStyle style, int delayMs) : this(-1, -1, message, null, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Initializes a spinner with a message, style, and color.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="style">The spinner style.</param>
    /// <param name="color">The foreground color to use.</param>
    public ConsoleSpinner(string message, SpinnerStyle style, ConsoleColor color) : this(-1, -1, message, color, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with a message, style, color, and delay.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="style">The spinner style.</param>
    /// <param name="color">The foreground color to use.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string message, SpinnerStyle style, ConsoleColor color, int delayMs) : this(-1, -1, message, color, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Initializes a spinner with a message and color.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="color">Foreground color.</param>
    public ConsoleSpinner(string message, ConsoleColor color) : this(-1, -1, message, color, null, null) { }

    /// <summary>
    /// Initializes a spinner with a message, color, and delay.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string message, ConsoleColor color, int delayMs) : this(-1, -1, message, color, null, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a message, color, and explicit character sequence.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="spinChars">Sequence of characters cycled to animate the spinner.</param>
    public ConsoleSpinner(string message, ConsoleColor color, string spinChars) : this(-1, -1, message, color, spinChars, null) { }

    /// <summary>
    /// Initializes a spinner with a message, color, character sequence, and delay.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string message, ConsoleColor color, string spinChars, int delayMs) : this(-1, -1, message, color, spinChars, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a message and explicit character sequence.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    public ConsoleSpinner(string message, string spinChars) : this(-1, -1, message, null, spinChars, null) { }

    /// <summary>
    /// Initializes a spinner with a message, character sequence, and delay.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string message, string spinChars, int delayMs) : this(-1, -1, message, null, spinChars, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a delay (default style, dynamic position).
    /// </summary>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int delayMs) : this(-1, -1, null, null, null, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a character sequence and delay.
    /// </summary>
    /// <param name="spinChars">Spinner frame characters.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(string spinChars, int delayMs) : this(-1, -1, null, null, spinChars, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a color (default style, dynamic position).
    /// </summary>
    /// <param name="color">Foreground color.</param>
    public ConsoleSpinner(ConsoleColor color) : this(-1, -1, null, color, null, null) { }

    /// <summary>
    /// Initializes a spinner with a color and delay.
    /// </summary>
    /// <param name="color">Foreground color.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(ConsoleColor color, int delayMs) : this(-1, -1, null, color, null, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a color, character sequence, and delay.
    /// </summary>
    /// <param name="color">Foreground color.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(ConsoleColor color, string spinChars, int delayMs) : this(-1, -1, null, color, spinChars, delayMs) { }

    /// <summary>
    /// Initializes a spinner with a predefined style.
    /// </summary>
    /// <param name="style">Spinner style.</param>
    public ConsoleSpinner(SpinnerStyle style) : this(-1, -1, null, null, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with a style and delay.
    /// </summary>
    /// <param name="style">Spinner style.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(SpinnerStyle style, int delayMs) : this(-1, -1, null, null, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Initializes a spinner with a style and color.
    /// </summary>
    /// <param name="style">Spinner style.</param>
    /// <param name="color">Foreground color.</param>
    public ConsoleSpinner(SpinnerStyle style, ConsoleColor color) : this(-1, -1, null, color, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with a style, color, and delay.
    /// </summary>
    /// <param name="style">Spinner style.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(SpinnerStyle style, ConsoleColor color, int delayMs) : this(-1, -1, null, color, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Initializes a spinner with explicit positional information (dynamic message/style).
    /// </summary>
    /// <param name="positionX">Column index or -1 for dynamic current column.</param>
    /// <param name="positionY">Row index or -1 for dynamic current row.</param>
    public ConsoleSpinner(int positionX, int positionY) : this(positionX, positionY, null, null, null, null) { }

    /// <summary>
    /// Initializes a spinner with position and delay.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int positionX, int positionY, int delayMs) : this(positionX, positionY, null, null, null, delayMs) { }

    /// <summary>
    /// Initializes a spinner with position and color.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="color">Foreground color.</param>
    public ConsoleSpinner(int positionX, int positionY, ConsoleColor color) : this(positionX, positionY, null, color, null, null) { }

    /// <summary>
    /// Initializes a spinner with position, color, and delay.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, int delayMs) : this(positionX, positionY, null, color, null, delayMs) { }

    /// <summary>
    /// Initializes a spinner with position, color, and custom character sequence.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, string spinChars) : this(positionX, positionY, null, color, spinChars, null) { }

    /// <summary>
    /// Initializes a spinner with position, color, predefined style.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="style">Spinner style.</param>
    public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, SpinnerStyle style) : this(positionX, positionY, null, color, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with position, color, style, and delay.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="color">Foreground color.</param>
    /// <param name="style">Spinner style.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, SpinnerStyle style, int delayMs) : this(positionX, positionY, null, color, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Initializes a spinner with position and custom character sequence.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    public ConsoleSpinner(int positionX, int positionY, string spinChars) : this(positionX, positionY, null, null, spinChars, null) { }

    /// <summary>
    /// Initializes a spinner with position, character sequence, and delay.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="spinChars">Spinner frame characters.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int positionX, int positionY, string spinChars, int delayMs) : this(positionX, positionY, null, null, spinChars, delayMs) { }

    /// <summary>
    /// Initializes a spinner with position and style.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="style">Spinner style.</param>
    public ConsoleSpinner(int positionX, int positionY, SpinnerStyle style) : this(positionX, positionY, null, null, GetSpinnerChars(style), null) { }

    /// <summary>
    /// Initializes a spinner with position, style, and delay.
    /// </summary>
    /// <param name="positionX">Column index or -1.</param>
    /// <param name="positionY">Row index or -1.</param>
    /// <param name="style">Spinner style.</param>
    /// <param name="delayMs">Delay between frames (ms).</param>
    public ConsoleSpinner(int positionX, int positionY, SpinnerStyle style, int delayMs) : this(positionX, positionY, null, null, GetSpinnerChars(style), delayMs) { }

    /// <summary>
    /// Primary constructor used internally by all other overloads.
    /// </summary>
    /// <param name="positionX">Column index or -1 for dynamic.</param>
    /// <param name="positionY">Row index or -1 for dynamic.</param>
    /// <param name="message">Optional message text.</param>
    /// <param name="color">Optional spinner color.</param>
    /// <param name="spinChars">Character sequence for animation. If null, defaults based on <see cref="SpinnerStyle.Braille"/> or ASCII fallback.</param>
    /// <param name="delayMs">Delay between frames (ms). Defaults to 100 if null.</param>
    public ConsoleSpinner(int positionX, int positionY, string? message, ConsoleColor? color, string? spinChars, int? delayMs)
    {
        PositionX = positionX;
        PositionY = positionY;
        Delay = delayMs ?? 100;
        Color = color ?? Console.ForegroundColor;
        _spinChars = spinChars ?? GetSpinnerChars(SpinnerStyle.Braille);
        _message = message ?? string.Empty;

        if (!ConsoleUtil.IsConsoleAvailable) return;

        _isSpinning = true;
        _spinThread = new Thread(Spin) { IsBackground = true };
        _spinThread.Start();
    }

    /// <summary>
    /// Selects the character sequence for the specified spinner style,
    /// </summary>
    /// <param name="style">The spinner style.</param>
    /// <returns>The character sequence for the spinner animation.</returns>
    private static string GetSpinnerChars(SpinnerStyle style)
    {
        style = IsUnicodeSupported ? style : SpinnerStyle.ASCII;
        return style switch
        {
            SpinnerStyle.ASCII => "|/-\\",
            SpinnerStyle.Braille => "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏",
            SpinnerStyle.Circles => "◐◓◑◒",
            SpinnerStyle.Arrows => "←↖↑↗→↘↓↙",
            SpinnerStyle.SimpleStars => "✶✸✹✺✹✷",
            SpinnerStyle.UnicodeStars => "✶✸✹✺✹✷❋❂❉✼✽✾",
            _ => "|/-\\",
        };
    }

    #endregion

    #region Methods

    /// <summary>
    /// Starts the spinner if it is currently stopped.
    /// </summary>
    /// <remarks>
    /// If the spinner was constructed with an active console, it begins immediately;
    /// calling <see cref="Start"/> after construction only has an effect if <see cref="Stop"/> was previously called.
    /// </remarks>
    public void Start()
    {
        lock (_lockObject)
        {
            if (!_isSpinning)
            {
                _isSpinning = true;
                _spinThread?.Start();
            }
        }
    }

    /// <summary>
    /// Stops the spinner and clears its last frame (including the message) from the console line.
    /// </summary>
    public void Stop()
    {
        lock (_lockObject)
        {
            _isSpinning = false;
            if (!ConsoleUtil.IsConsoleAvailable) return;
            lock (_globalConsoleLock)
            {
                var x = PositionX == -1 ? Console.WindowLeft : PositionX;
                var y = PositionY == -1 ? Console.CursorTop : PositionY;
                Console.SetCursorPosition(x, y);
                var clearLength = 1;
                if (!string.IsNullOrEmpty(_message)) clearLength += 1 + _message.Length;
                Console.Write(new string(' ', clearLength));
            }
        }
    }

    /// <summary>
    /// Background thread loop updating the spinner frame until <see cref="Stop"/> is invoked.
    /// </summary>
    private void Spin()
    {
        if (!ConsoleUtil.IsConsoleAvailable) return;
        var index = 0;
        while (_isSpinning)
        {
            lock (_lockObject)
            {
                if (!_isSpinning) break;
                lock (_globalConsoleLock)
                {
                    var currentTop = Console.CursorTop;
                    var currentLeft = Console.CursorLeft;
                    var originalColor = Console.ForegroundColor;
                    var y = PositionY == -1 ? Console.CursorTop : PositionY;
                    var x = PositionX == -1 ? Console.WindowLeft : PositionX;
                    Console.ForegroundColor = Color ?? originalColor;
                    Console.SetCursorPosition(x, y);
                    var output = _spinChars[index].ToString();
                    if (!string.IsNullOrEmpty(_message)) output += " " + _message;
                    Console.Write(output);
                    Console.ForegroundColor = originalColor;
                    Console.SetCursorPosition(currentLeft, currentTop);
                }
            }
            index = (index + 1) % _spinChars.Length;
            Thread.Sleep(Delay);
        }
    }

    /// <summary>
    /// Stops the spinner and waits (up to 1 second) for the background animation thread to terminate.
    /// </summary>
    public void Dispose()
    {
        Stop();
        if (_spinThread != null && _spinThread.IsAlive) _spinThread.Join(1000);
    }

    #endregion
}

/// <summary>
/// Defines available spinner animation styles.
/// </summary>
public enum SpinnerStyle
{
    /// <summary>
    /// Basic ASCII characters ("|/-\"). Guaranteed to render in any console.
    /// </summary>
    ASCII,

    /// <summary>
    /// Braille-inspired dot pattern sequence for smooth animation.
    /// </summary>
    Braille,

    /// <summary>
    /// Rotating circle quadrants ("◐◓◑◒").
    /// </summary>
    /// 
    Circles,

    /// <summary>
    /// Directional arrow rotation ("←↖↑↗→↘↓↙").
    /// </summary>
    /// 
    Arrows,

    /// <summary>
    /// Simple star burst cycle.
    /// </summary>
    SimpleStars,

    /// <summary>
    /// Decorative Unicode star sequence.
    /// </summary>
    UnicodeStars,

    /// <summary>
    /// Alias for <see cref="Braille"/>.
    /// </summary>
    Dots = Braille,

    /// <summary>
    /// Alias for <see cref="ASCII"/> (default style).
    /// </summary>
    Default = ASCII,
}
