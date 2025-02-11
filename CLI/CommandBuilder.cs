using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reflection;

namespace ByteForge.Toolkit.CommandLine
{
    /// <summary>
    /// Class to build commands from an assembly.
    /// </summary>
    public class CommandBuilder
    {
        private static readonly HashSet<Type> ValidParameterTypes = new HashSet<Type>()
        {
            typeof(string),     typeof(int),        typeof(long),       typeof(float),
            typeof(double),     typeof(decimal),    typeof(bool),       typeof(DateTime),
            typeof(Guid),       typeof(string[]),   typeof(int[]),      typeof(long[]),
            typeof(float[]),    typeof(double[]),   typeof(decimal[]),  typeof(bool[]),
            typeof(DateTime[]), typeof(Guid[])
        };

        /// <summary>
        /// Builds commands from the specified assembly path.
        /// </summary>
        /// <param name="assemblyPath">The path to the assembly.</param>
        /// <returns>A collection of commands.</returns>
        public static IEnumerable<Command> BuildFromAssembly(string assemblyPath)
        {
            var assembly = Assembly.LoadFrom(assemblyPath);
            return BuildFromAssembly(assembly);
        }

        /// <summary>
        /// Builds commands from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to build commands from.</param>
        /// <returns>A collection of commands.</returns>
        public static IEnumerable<Command> BuildFromAssembly(Assembly assembly)
        {
            var commands = new List<Command>();

            foreach (var type in assembly.GetTypes())
            {
                var cmdAttr = type.GetCustomAttribute<CommandAttribute>();
                if (cmdAttr != null)
                {
                    commands.Add(BuildCommandGroup(type, cmdAttr));
                }
            }

            return commands;
        }

        /// <summary>
        /// Builds a command group from the specified type and command attribute.
        /// </summary>
        /// <param name="type">The type to build the command group from.</param>
        /// <param name="groupAttr">The command attribute for the group.</param>
        /// <returns>The built command group.</returns>
        private static Command BuildCommandGroup(Type type, CommandAttribute groupAttr)
        {
            var groupCommand = new Command(groupAttr.Name, groupAttr.Description);
            var groupTracker = new NameTracker(); // Track names across all commands in group

            if (groupAttr.Aliases?.Length > 0)
            {
                if (!groupTracker.TryAddNames(groupAttr.Aliases))
                {
                    Log.Warning($"Command group {groupAttr.Name} has duplicate aliases. Skipping alias assignment.");
                }
                else
                {
                    foreach (var alias in groupAttr.Aliases)
                        groupCommand.AddAlias(alias);
                }
            }

            var instance = Activator.CreateInstance(type);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var cmdAttr = method.GetCustomAttribute<CommandAttribute>();
                if (cmdAttr == null) continue;

                if (!ValidateMethodParameters(method, out var error))
                {
                    Log.Warning($"Skipping command {cmdAttr.Name}: {error}");
                    continue;
                }

                // Verify command name and aliases don't conflict with group
                if (!groupTracker.TryAddName(cmdAttr.Name))
                {
                    Log.Warning($"Command name {cmdAttr.Name} conflicts with existing name in group. Skipping command.");
                    continue;
                }

                var command = new Command(cmdAttr.Name, cmdAttr.Description);

                if (cmdAttr.Aliases?.Length > 0)
                {
                    if (!groupTracker.TryAddNames(cmdAttr.Aliases))
                    {
                        Log.Warning($"Command {cmdAttr.Name} has conflicting aliases. Skipping alias assignment.");
                    }
                    else
                    {
                        foreach (var alias in cmdAttr.Aliases)
                            command.AddAlias(alias);
                    }
                }

                if (AddOptionsFromParameters(command, method))
                {
                    command.Handler = CommandHandler.Create(method, instance);
                    groupCommand.AddCommand(command);
                }
                else
                {
                    // Remove the command's names from tracker since we're skipping it
                    groupTracker.TryAddName(cmdAttr.Name);
                    if (cmdAttr.Aliases != null)
                    {
                        foreach (var alias in cmdAttr.Aliases)
                            groupTracker.TryAddName(alias);
                    }
                }
            }

            return groupCommand;
        }

        /// <summary>
        /// Validates the parameters of the specified method.
        /// </summary>
        /// <param name="method">The method to validate parameters for.</param>
        /// <param name="error">The error message if validation fails.</param>
        /// <returns>True if the parameters are valid, otherwise false.</returns>
        private static bool ValidateMethodParameters(MethodInfo method, out string error)
        {
            foreach (var param in method.GetParameters())
            {
                var paramType = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;

                // Check if it's an array
                if (paramType.IsArray)
                {
                    error = $"Parameter {param.Name} is an array, which is not supported";
                    return false;
                }

                if (!ValidParameterTypes.Contains(paramType))
                {
                    error = $"Parameter {param.Name} has unsupported type {param.ParameterType}";
                    return false;
                }
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Adds options to the command from the parameters of the specified method.
        /// </summary>
        /// <param name="command">The command to add options to.</param>
        /// <param name="method">The method to get parameters from.</param>
        private static bool AddOptionsFromParameters(Command command, MethodInfo method)
        {
            var optionTracker = new NameTracker(); // Track names within this command

            foreach (var param in method.GetParameters())
            {
                var optAttr = param.GetCustomAttribute<OptionAttribute>();
                var paramType = param.ParameterType;
                var isNullable = !paramType.IsValueType || Nullable.GetUnderlyingType(paramType) != null;
                var optionName = (optAttr?.Name ?? param.Name).TrimStart('-', '/');

                // Verify option name doesn't conflict
                if (!optionTracker.TryAddName(optionName))
                {
                    Log.Warning($"Parameter {param.Name} in command {command.Name} has conflicting name. Skipping command.");
                    return false;
                }

                // Create the option with the correct type
                var option = CreateTypedOption(paramType, optionName, optAttr?.Description ?? $"The {param.Name} parameter");
                option.IsRequired = !isNullable;

                // Get both generated and developer-provided aliases
                var generatedAliases = GenerateAliases(optionName).Concat(option.Aliases);
                var devAliases = NormalizeAlias(optAttr?.Aliases);
                var allAliases = generatedAliases.Concat(devAliases).Distinct().ToList();
                var cm = optionTracker.Intersect(allAliases);
                allAliases.RemoveAll(x => cm.Contains(x));
                optionTracker.AddNames(allAliases);

                foreach (var alias in allAliases)
                    option.AddAlias(alias);

                command.AddOption(option);
            }

            return true;
        }

        /// <summary>
        /// Normalizes the provided aliases by trimming leading characters and generating standard aliases.
        /// </summary>
        /// <param name="aliases">The aliases to normalize.</param>
        /// <returns>A collection of normalized aliases.</returns>
        private static IEnumerable<string> NormalizeAlias(string[] aliases)
        {
            if (aliases == null || aliases.Length == 0) return Array.Empty<string>();
            return aliases.Select(a => a.TrimStart('-', '/')).SelectMany(a => GenerateAliases(a)).Distinct();
        }

        /// <summary>
        /// Creates a typed option for the specified parameter type, name, and description.
        /// </summary>
        /// <param name="paramType">The parameter type.</param>
        /// <param name="name">The name of the option.</param>
        /// <param name="description">The description of the option.</param>
        /// <returns>The created option.</returns>
        private static Option CreateTypedOption(Type paramType, string name, string description)
        {
            var underlyingType = Nullable.GetUnderlyingType(paramType) ?? paramType;
            if (underlyingType.IsArray)
                throw new ArgumentException("Arrays are not supported as command parameters");

            string[] aliases = new string[] { "--" + name };

            // Handle scalar types
            if (underlyingType == typeof(string))         return new Option<string>(aliases, description);
            else if (underlyingType == typeof(int))       return new Option<int>(aliases, description);
            else if (underlyingType == typeof(long))      return new Option<long>(aliases, description);
            else if (underlyingType == typeof(float))     return new Option<float>(aliases, description);
            else if (underlyingType == typeof(double))    return new Option<double>(aliases, description);
            else if (underlyingType == typeof(decimal))   return new Option<decimal>(aliases, description);
            else if (underlyingType == typeof(bool))      return new Option<bool>(aliases, description);
            else if (underlyingType == typeof(DateTime))  return new Option<DateTime>(aliases, description);
            else if (underlyingType == typeof(Guid))      return new Option<Guid>(aliases, description);
            else
                throw new ArgumentException($"Unsupported parameter type: {paramType}");
        }

        /// <summary>
        /// Generates aliases for the specified option name.
        /// </summary>
        /// <param name="option">The option name.</param>
        /// <returns>An array of aliases.</returns>
        private static string[] GenerateAliases(string option)
        {
            // Create short name based on option length
            var shortName = option.Length >= 3
                ? option.Substring(0, 3).ToLowerInvariant()
                : option.ToLowerInvariant();

            // Generate standard aliases with single character and short name
            var standardAliases = new[]
            {
        $"/{shortName[0]}",              // Single char with /
        $"-{shortName[0]}",              // Single char with -
        $"--{option.ToLowerInvariant()}" // Full name with --
    };
            return standardAliases;
        }

        /// <summary>
        /// Tracks the names used to ensure no duplicates.
        /// </summary>
        private class NameTracker : IEnumerable<string>
        {
            private readonly HashSet<string> _usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Tries to add a name to the tracker.
            /// </summary>
            /// <param name="name">The name to add.</param>
            /// <returns>True if the name was added, otherwise false.</returns>
            public bool TryAddName(string name) => _usedNames.Add(name);

            /// <summary>
            /// Tries to add multiple names to the tracker.
            /// </summary>
            /// <param name="names">The names to add.</param>
            /// <returns>True if all names were added, otherwise false.</returns>
            public bool TryAddNames(IEnumerable<string> names)
            {
                var allNew = true;
                foreach (var name in names)
                {
                    if (!TryAddName(name))
                    {
                        allNew = false;
                        break;
                    }
                }

                // If any conflicts, remove all names we just tried to add
                if (!allNew)
                {
                    foreach (var name in names)
                    {
                        _usedNames.Remove(name);
                    }
                }

                return allNew;
            }

            /// <summary>
            /// Adds multiple names to the tracker without checking for conflicts.
            /// </summary>
            /// <param name="names">The names to add.</param>
            public void AddNames(IEnumerable<string> names)
            {
                foreach (var name in names)
                    _usedNames.Add(name);
            }

            /// <summary>
            /// Clears all tracked names.
            /// </summary>
            public void Clear() => _usedNames.Clear();

            /// <summary>
            /// Returns an enumerator that iterates through the collection of used names.
            /// </summary>
            /// <returns>An enumerator for the collection of used names.</returns>
            public IEnumerator<string> GetEnumerator()
            {
                return ((IEnumerable<string>)_usedNames).GetEnumerator();
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection of used names.
            /// </summary>
            /// <returns>An enumerator for the collection of used names.</returns>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)_usedNames).GetEnumerator();
            }
        }

    }
}