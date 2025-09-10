using ByteForge.Toolkit.CLI;
using System;
using System.Collections.Generic;
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
        private bool _useHelp = true;
        private bool _useEnvironmentVariables = true;
        private bool _useParseDirective = true;
        private bool _useSuggestDirective = true;
        private bool _useTypoCorrections = true;
        private bool _useParseErrorReporting = true;
        private bool _useExceptionHandler = true;
        private bool _useCancellation = true;

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
        /// <returns>The builder instance for method chaining.</returns>
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
        /// <returns>The builder instance for method chaining.</returns>
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
        /// <returns>The builder instance for method chaining.</returns>
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
        /// <returns>The builder instance for method chaining.</returns>
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
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder AddCommand(Command command)
        {
            _rootCommand.AddCommand(command);
            return this;
        }

        /// <summary>
        /// Configures whether to use the help directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseHelp(bool enable = true)
        {
            _useHelp = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use environment variables.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseEnvironmentVariables(bool enable = true)
        {
            _useEnvironmentVariables = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the parse directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseParseDirective(bool enable = true)
        {
            _useParseDirective = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the suggest directive.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseSuggestDirective(bool enable = true)
        {
            _useSuggestDirective = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use typo corrections.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseTypoCorrections(bool enable = true)
        {
            _useTypoCorrections = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use parse error reporting.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseParseErrorReporting(bool enable = true)
        {
            _useParseErrorReporting = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use the exception handler.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseExceptionHandler(bool enable = true)
        {
            _useExceptionHandler = enable;
            return this;
        }

        /// <summary>
        /// Configures whether to use cancellation on process termination.
        /// </summary>
        /// <param name="enable">Whether to enable the feature.</param>
        /// <returns>The builder instance for method chaining.</returns>
        public RootCommandBuilder UseCancellation(bool enable = true)
        {
            _useCancellation = enable;
            return this;
        }

        /// <summary>
        /// Builds and returns the command line parser.
        /// </summary>
        /// <returns>The configured parser.</returns>
        public CommandParser Build()
        {
            var builder = new CommandLineBuilder(_rootCommand);

            if (_useHelp)
                builder.UseHelp();

            if (_useEnvironmentVariables)
                builder.UseEnvironmentVariableDirective();

            if (_useParseDirective)
                builder.UseParseDirective();

            if (_useSuggestDirective)
            {
                builder.UseSuggestDirective();
                builder.RegisterWithDotnetSuggest();
            }

            if (_useTypoCorrections)
                builder.UseTypoCorrections();

            if (_useParseErrorReporting)
                builder.UseParseErrorReporting();

            if (_useExceptionHandler)
                builder.UseExceptionHandler();

            if (_useCancellation)
                builder.CancelOnProcessTermination();

            var systemParser = builder.Build();
            var tokenList = CommandBuilder.TokenList;
            return new CommandParser(systemParser, tokenList);
        }
    }
}