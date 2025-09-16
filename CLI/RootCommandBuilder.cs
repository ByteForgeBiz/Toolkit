using ByteForge.Toolkit.CLI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using System.Reflection;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Fluent builder for creating command line parsers.
    /// </summary>
    public class RootCommandBuilder
    {
        private readonly RootCommand _rootCommand;
        private readonly HashSet<string> _loadedAssemblies = new HashSet<string>();
        private readonly List<GlobalOption> _globalOptions = new List<GlobalOption>();

        /// <summary>
        /// Gets or sets a value indicating whether help is enabled for the command line parser.
        /// </summary>
        public bool EnableHelp { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether environment variable directive is enabled for the command line parser.
        /// </summary>
        public bool EnableEnvironmentVariables { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the parse directive is enabled for the command line parser.
        /// </summary>
        public bool EnableParseDirective { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the suggest directive is enabled for the command line parser.
        /// </summary>
        public bool EnableSuggestDirective { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether typo corrections are enabled for the command line parser.
        /// </summary>
        public bool EnableTypoCorrections { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether parse error reporting is enabled for the command line parser.
        /// </summary>
        public bool EnableParseErrorReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the exception handler is enabled for the command line parser.
        /// </summary>
        public bool EnableExceptionHandler { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether cancellation on process termination is enabled for the command line parser.
        /// </summary>
        public bool EnableCancellation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether case sensitivity is enabled for command parsing.
        /// </summary>
        public bool EnableCaseSensitivity { get; set; }

        /// <summary>
        /// Gets or sets the action to be executed to display a banner before command parsing.
        /// </summary>
        public Action BannerAction { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether parameter explanation is enabled.
        /// When enabled, details about parsed commands and arguments are displayed to the user.
        /// </summary>
        public bool EnableParameterExplanation { get; set; }

        /// <summary>
        /// Gets the collection of global options configured for this command builder.
        /// </summary>
        public IReadOnlyList<GlobalOption> GlobalOptions => _globalOptions.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the <see cref="RootCommandBuilder"/> class.
        /// </summary>
        /// <param name="description">The description of the root command.</param>
        public RootCommandBuilder(string description)
        {
            _rootCommand = new RootCommand(description);
        }

        /// <summary>
        /// Adds commands from the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly file.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddAssembly(string assemblyPath)
        {
            if (!_loadedAssemblies.Contains(assemblyPath))
            {
                try
                {
                    foreach (var command in CommandBuilder.BuildFromAssembly(assemblyPath))
                    {
                        _rootCommand.AddCommand(command);
                    }
                    _loadedAssemblies.Add(assemblyPath);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to load assembly {assemblyPath}: {ex.Message}");
                }
            }
            return this;
        }

        /// <summary>
        /// Adds commands from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to load commands from.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddAssembly(Assembly assembly)
        {
            var assemblyPath = assembly.Location;
            if (!_loadedAssemblies.Contains(assemblyPath))
            {
                foreach (var command in CommandBuilder.BuildFromAssembly(assembly))
                {
                    _rootCommand.AddCommand(command);
                }
                _loadedAssemblies.Add(assemblyPath);
            }
            return this;
        }

        /// <summary>
        /// Searches for and loads plugins from the default plugins directory.
        /// </summary>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder SearchPlugins()
        {
            var pluginsDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "plugins"
            );
            return SearchPlugins(pluginsDir);
        }

        /// <summary>
        /// Searches for and loads plugins from the specified directory.
        /// </summary>
        /// <param name="pluginsPath">The directory to search for plugins.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder SearchPlugins(string pluginsPath)
        {
            if (Directory.Exists(pluginsPath))
            {
                foreach (var pluginFile in Directory.GetFiles(pluginsPath, "*.dll"))
                {
                    AddAssembly(pluginFile);
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a specific command to the root command.
        /// </summary>
        /// <param name="command">The command to add.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddCommand(Command command)
        {
            _rootCommand.AddCommand(command);
            return this;
        }

        /// <summary>
        /// Configures a banner action to be executed before command parsing.
        /// </summary>
        /// <param name="bannerAction">The action to execute to display the banner.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder WithBanner(Action bannerAction)
        {
            BannerAction = bannerAction;
            return this;
        }

        /// <summary>
        /// Enables parameter explanation functionality that displays parsed command details.
        /// </summary>
        /// <param name="enable">Whether to enable parameter explanation.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseParameterExplanation(bool enable = true)
        {
            EnableParameterExplanation = enable;
            return this;
        }

        /// <summary>
        /// Adds a global option that is processed before command parsing and available to all commands.
        /// </summary>
        /// <param name="name">The name of the global option (without dashes).</param>
        /// <param name="description">The description of the global option.</param>
        /// <param name="action">The action to execute when the option is encountered.</param>
        /// <param name="aliases">Custom aliases for the option.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddGlobalOption(string name, string description, Action action, params string[] aliases)
        {
            var option = new GlobalOption(name, description, action, false, aliases);
            _globalOptions.Add(option);
            return this;
        }

        /// <summary>
        /// Adds a global option that expects a value and is processed before command parsing.
        /// </summary>
        /// <param name="name">The name of the global option (without dashes).</param>
        /// <param name="description">The description of the global option.</param>
        /// <param name="action">The action to execute when the option is encountered, receiving the option value.</param>
        /// <param name="aliases">Custom aliases for the option.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddGlobalOption(string name, string description, Action<string> action, params string[] aliases)
        {
            var option = new GlobalOption(name, description, action, true, aliases);
            _globalOptions.Add(option);
            return this;
        }

        /// <summary>
        /// Adds common global options for console logging control.
        /// Adds --console/-c to enable console logging and --quiet/-q to disable it.
        /// </summary>
        /// <param name="enableConsoleAction">Action to execute when console logging should be enabled.</param>
        /// <param name="disableConsoleAction">Action to execute when console logging should be disabled.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder AddConsoleLoggingOptions(Action enableConsoleAction = null, Action disableConsoleAction = null)
        {
            if (enableConsoleAction != null)
                AddGlobalOption("console", "Enable console logging output", enableConsoleAction, "c");
            
            if (disableConsoleAction != null)
                AddGlobalOption("quiet", "Suppress all console logging except errors", disableConsoleAction, "q");
            
            return this;
        }

        /// <summary>
        /// Configures whether to use the help directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseHelp(bool enable = true)
        {
            EnableHelp = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use environment variables.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseEnvironmentVariables(bool enable = true)
        {
            EnableEnvironmentVariables = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the parse directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseParseDirective(bool enable = true)
        {
            EnableParseDirective = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the suggest directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseSuggestDirective(bool enable = true)
        {
            EnableSuggestDirective = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use typo corrections.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseTypoCorrections(bool enable = true)
        {
            EnableTypoCorrections = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use parse error reporting.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseParseErrorReporting(bool enable = true)
        {
            EnableParseErrorReporting = enable;
            return this;
        }

        /// <summary>
        /// Configures the case sensitivity for command parsing, including command names, options, and arguments.<br/>
        /// The default is case-insensitive.
        /// </summary>
        /// <param name="enable">A value indicating whether case sensitivity should be enabled.<br/>
        /// The default is <see langword="false"/>, meaning that command parsing is case-insensitive.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseCaseSensitivity(bool enable = false)
        {
            EnableCaseSensitivity = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the exception handler.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseExceptionHandler(bool enable = true)
        {
            EnableExceptionHandler = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use cancellation on process termination.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The current <see cref="RootCommandBuilder"/> instance, allowing for method chaining.</returns>
        public RootCommandBuilder UseCancellation(bool enable = true)
        {
            EnableCancellation = enable;
            return this;
        }

        /// <summary>
        /// Builds and returns the command line parser.
        /// </summary>
        /// <returns>The configured parser.</returns>
        public CommandParser Build()
        {
            // Generate aliases for global options
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var globalOption in _globalOptions)
            {
                globalOption.GenerateAliases(usedNames);
            }

            var builder = new CommandLineBuilder(_rootCommand);

            if (EnableHelp)
                builder.UseHelp();

            if (EnableEnvironmentVariables)
                builder.UseEnvironmentVariableDirective();

            if (EnableParseDirective)
                builder.UseParseDirective();

            if (EnableSuggestDirective)
            {
                builder.UseSuggestDirective();
                builder.RegisterWithDotnetSuggest();
            }

            if (EnableTypoCorrections)
                builder.UseTypoCorrections();

            if (EnableParseErrorReporting)
                builder.UseParseErrorReporting();

            if (EnableExceptionHandler)
                builder.UseExceptionHandler();

            if (EnableCancellation)
                builder.CancelOnProcessTermination();

            var systemParser = builder.Build();
            var tokenList = CommandBuilder.TokenList;

            if (EnableCaseSensitivity)
                tokenList = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());

            return new CommandParser(this, systemParser, tokenList);
        }
    }
}