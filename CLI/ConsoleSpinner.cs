using System;
using System.Threading;

namespace ByteForge.Toolkit
{
    /// <summary>
    /// Provides a console-based spinner animation at a fixed position.<br/>
    /// Useful for indicating progress in console applications.
    /// </summary>
    /// <remarks>
    /// If Unicode is not supported by the current console (e.g., on some Windows environments or when output encoding is not UTF-8),
    /// the spinner gracefully falls back to an ASCII style regardless of the requested spinner style.
    /// This ensures the spinner remains visible and functional even in limited console environments.
    /// Spinner character selection is handled at construction time via the <c>GetSpinnerChars</c> method.
    /// </remarks>
    public class ConsoleSpinner : IDisposable
    {
        #region Fields

        /// <summary>
        /// Global lock object to synchronize console access across threads.
        /// </summary>
        private static readonly object _globalConsoleLock = new object();

        /// <summary>
        /// Characters used for the spinner animation.
        /// </summary>
        private readonly string _spinChars;

        /// <summary>
        /// The thread responsible for running the spinner animation.
        /// </summary>
        private readonly Thread _spinThread;

        /// <summary>
        /// Indicates whether the spinner is currently running.
        /// </summary>
        private volatile bool _isSpinning;

        /// <summary>
        /// Instance-level lock object for thread safety.
        /// </summary>
        private readonly object _lockObject = new object();

        /// <summary>
        /// Backing field for the <see cref="Color"/> property.
        /// </summary>
        private ConsoleColor? _color;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the color to be used for console output.
        /// Thread-safe.
        /// </summary>
        public ConsoleColor? Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (_color == value) return;
                lock (_lockObject)
                {
                    _color = value;
                }
            }
        }

        /// <summary>
        /// Gets the X (column) position for the spinner.
        /// </summary>
        public int PositionX { get; }

        /// <summary>
        /// Gets the Y (row) position for the spinner.
        /// </summary>
        public int PositionY { get; }

        /// <summary>
        /// Gets the delay in milliseconds between spinner frames.
        /// </summary>
        public int Delay { get; }

        /// <summary>
        /// Gets a value indicating whether Unicode is supported on the current platform.
        /// </summary>
        private static bool IsUnicodeSupported => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX || Console.OutputEncoding.BodyName == "utf-8";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with default settings.
        /// </summary>
        public ConsoleSpinner() : this(-1, -1, null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified delay.
        /// </summary>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int delayMs) : this(-1, -1, null, null, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with custom spinner characters.
        /// </summary>
        /// <param name="spinChars">A string containing spinner characters.</param>
        public ConsoleSpinner(string spinChars) : this(-1, -1, null, spinChars, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with custom spinner characters and delay.
        /// </summary>
        /// <param name="spinChars">A string containing spinner characters.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(string spinChars, int delayMs) : this(-1, -1, null, spinChars, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified color.
        /// </summary>
        /// <param name="color">The color to be used for console output.</param>
        public ConsoleSpinner(ConsoleColor color) : this(-1, -1, color, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified color and delay.
        /// </summary>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(ConsoleColor color, int delayMs) : this(-1, -1, color, null, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified color, custom spinner characters, and delay.
        /// </summary>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="spinChars">A string containing spinner characters.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(ConsoleColor color, string spinChars, int delayMs) : this(-1, -1, color, spinChars, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified color, spinner style, and delay.
        /// </summary>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="style">The spinner style.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(ConsoleColor color, SpinnerStyle style, int delayMs) : this(-1, -1, color, GetSpinnerChars(style), delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified spinner style.
        /// </summary>
        /// <param name="style">The spinner style.</param>
        public ConsoleSpinner(SpinnerStyle style) : this(-1, -1, null, GetSpinnerChars(style), null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with a specified spinner style and delay.
        /// </summary>
        /// <param name="style">The spinner style.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(SpinnerStyle style, int delayMs) : this(-1, -1, null, GetSpinnerChars(style), delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        public ConsoleSpinner(int positionX, int positionY) : this(positionX, positionY, null, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a delay.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, int delayMs) : this(positionX, positionY, null, null, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a color.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor color) : this(positionX, positionY, color, null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a color and delay.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, int delayMs) : this(positionX, positionY, color, null, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a color and custom spinner characters.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="spinChars">A string containing spinner characters.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, string spinChars) : this(positionX, positionY, color, spinChars, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a color and spinner style.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="style">The spinner style.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, SpinnerStyle style) : this(positionX, positionY, color, GetSpinnerChars(style), null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a color, spinner style, and delay.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="style">The spinner style.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor color, SpinnerStyle style, int delayMs) : this(positionX, positionY, color, GetSpinnerChars(style), delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with custom spinner characters.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="spinChars">A string containing spinner characters.</param>
        public ConsoleSpinner(int positionX, int positionY, string spinChars) : this(positionX, positionY, null, spinChars, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with custom spinner characters and delay.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="spinChars">A string containing spinner characters.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, string spinChars, int delayMs) : this(positionX, positionY, null, spinChars, delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a spinner style.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="style">The spinner style.</param>
        public ConsoleSpinner(int positionX, int positionY, SpinnerStyle style) : this(positionX, positionY, null, GetSpinnerChars(style), null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class at a specified position with a spinner style and delay.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="style">The spinner style.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, SpinnerStyle style, int delayMs) : this(positionX, positionY, null, GetSpinnerChars(style), delayMs) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsoleSpinner"/> class with all parameters specified.
        /// </summary>
        /// <param name="positionX">The X (column) position for the spinner.</param>
        /// <param name="positionY">The Y (row) position for the spinner.</param>
        /// <param name="color">The color to be used for console output.</param>
        /// <param name="spinChars">A string containing spinner characters.</param>
        /// <param name="delayMs">The delay in milliseconds between spinner frames.</param>
        public ConsoleSpinner(int positionX, int positionY, ConsoleColor? color, string spinChars, int? delayMs)
        {
            PositionX = positionX;
            PositionY = positionY;
            Delay = delayMs ?? 100;
            Color = color ?? ConsoleColor.DarkYellow;
            _spinChars = !string.IsNullOrEmpty(spinChars) ? spinChars : GetSpinnerChars(SpinnerStyle.Braille);
            _spinThread = new Thread(Spin) { IsBackground = true };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the spinner characters for a given <see cref="SpinnerStyle"/>.
        /// </summary>
        /// <param name="style">The spinner style.</param>
        /// <returns>A string containing spinner characters.</returns>
        private static string GetSpinnerChars(SpinnerStyle style)
        {
            if (!IsUnicodeSupported)
                style = SpinnerStyle.ASCII;

            return style switch
            {
                SpinnerStyle.ASCII => "|/-\\",
                SpinnerStyle.Braille => "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏",
                SpinnerStyle.Circles => "◐◓◑◒",
                SpinnerStyle.Arrows => "←↖↑↗→↘↓↙",
                SpinnerStyle.SimpleStars => "✶✸✹✺✹✷",
                SpinnerStyle.UnicodeStars => "✦✧✩✪✫✬✭✮✯",
                _ => "|/-\\",
            };
        }

        /// <summary>
        /// Starts the spinner animation.
        /// </summary>
        public void Start()
        {
            lock (_lockObject)
            {
                if (!_isSpinning)
                {
                    _isSpinning = true;
                    _spinThread.Start();
                }
            }
        }

        /// <summary>
        /// Stops the spinner animation and clears the spinner character from the console.
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                _isSpinning = false;

                // It wasn't spinning, nothing to clear
                if (!ConsoleUtil.IsConsoleAvailable) return;

                // Clear the spinner character at the fixed position
                lock (_globalConsoleLock)
                {
                    var x = PositionX == -1 ? Console.WindowLeft : PositionX;
                    var y = PositionY == -1 ? Console.CursorTop : PositionY;

                    Console.SetCursorPosition(x, y);
                    Console.Write(' ');
                }
            }
        }

        /// <summary>
        /// The main spinner loop, responsible for updating the spinner character.
        /// </summary>
        private void Spin()
        {
            // No need to spin if console is redirected
            if (!ConsoleUtil.IsConsoleAvailable) return;

            var index = 0;

            while (_isSpinning)
            {
                lock (_lockObject)
                {
                    if (!_isSpinning) break;

                    // Use global lock to prevent cursor position conflicts
                    lock (_globalConsoleLock)
                    {
                        // Save current cursor position
                        var currentTop = Console.CursorTop;
                        var currentLeft = Console.CursorLeft;
                        var originalColor = Console.ForegroundColor;

                        var y = PositionY == -1 ? Console.CursorTop : PositionY;
                        var x = PositionX == -1 ? Console.WindowLeft : PositionX;

                        // Move to spinner position and draw
                        Console.ForegroundColor = Color ?? originalColor;
                        Console.SetCursorPosition(x, y);
                        Console.Write(_spinChars[index]);

                        // Restore cursor position
                        Console.ForegroundColor = originalColor;
                        Console.SetCursorPosition(currentLeft, currentTop);
                    }
                }

                index = (index + 1) % _spinChars.Length;
                Thread.Sleep(Delay);
            }
        }

        /// <summary>
        /// Disposes the spinner, stopping the animation and waiting for the thread to finish.
        /// </summary>
        public void Dispose()
        {
            Stop();

            if (_spinThread.IsAlive)
                _spinThread.Join(1000); // Wait up to 1 second for thread to finish
        }

        #endregion
    }

    /// <summary>
    /// Represents the available spinner styles for <see cref="ConsoleSpinner"/>.
    /// </summary>
    public enum SpinnerStyle
    {
        /// <summary>
        /// ASCII spinner style: | / - \
        /// </summary>
        ASCII,

        /// <summary>
        /// Braille spinner style: ⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏
        /// </summary>
        Braille,

        /// <summary>
        /// Circles spinner style: ◐◓◑◒
        /// </summary>
        Circles,

        /// <summary>
        /// Arrows spinner style: ←↖↑↗→↘↓↙
        /// </summary>
        Arrows,

        /// <summary>
        /// Simple stars spinner style: ✶✸✹✺✹✷
        /// </summary>
        SimpleStars,

        /// <summary>
        /// Unicode stars spinner style: ✦✧✩✪✫✬✭✮✯
        /// </summary>
        UnicodeStars,

        /// <summary>
        /// Dots spinner style (alias for Braille): ⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏
        /// </summary>
        Dots = Braille,
    }
}
