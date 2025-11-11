using ByteForge.Toolkit.CommandLine;
using System.Reflection;

namespace TestBed
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Testing CLI Examples from XML Documentation");
            
            // Test each example
            TestCommandAttribute();
            TestOptionAttribute();
            TestConsoleSpinner();
            TestProgressSpinner();
            TestGlobalOption();
            await TestRootCommandBuilder();
            await TestCompleteApplication(args);
            
            Console.WriteLine("All tests completed!");
            return 0;
        }

        // Sample 1: CommandAttribute with simple command
        static void TestCommandAttribute()
        {
            Console.WriteLine("✓ CommandAttribute classes defined");
        }

        // Sample 2: OptionAttribute with method parameters  
        static void TestOptionAttribute()
        {
            Console.WriteLine("✓ OptionAttribute classes defined");
        }

        // Sample 3: ConsoleSpinner tests
        static void TestConsoleSpinner()
        {
            Console.WriteLine("Testing ConsoleSpinner...");
            
            // Test basic spinner
            using (var spinner = new ConsoleSpinner("Testing spinner"))
            {
                Thread.Sleep(500);
            }
            
            // Test different styles
            var asciiSpinner = new ConsoleSpinner("Loading", SpinnerStyle.ASCII);
            var brailleSpinner = new ConsoleSpinner("Loading", SpinnerStyle.Braille);
            var circleSpinner = new ConsoleSpinner("Loading", SpinnerStyle.Circles);
            var arrowSpinner = new ConsoleSpinner("Loading", SpinnerStyle.Arrows);
            var starSpinner = new ConsoleSpinner("Loading", SpinnerStyle.UnicodeStars);
            
            // Custom spinner characters
            var customSpinner = new ConsoleSpinner("Working", "⣾⣽⣻⢿⡿⣟⣯⣷");
            
            // Positional spinner
            var positionedSpinner = new ConsoleSpinner(5, 10, "Loading at position (5,10)");
            
            // Dispose all spinners
            asciiSpinner.Dispose();
            brailleSpinner.Dispose();
            circleSpinner.Dispose();
            arrowSpinner.Dispose();
            starSpinner.Dispose();
            customSpinner.Dispose();
            positionedSpinner.Dispose();
            
            Console.WriteLine("✓ ConsoleSpinner tests completed");
        }

        // Sample 4: ProgressSpinner tests
        static void TestProgressSpinner()
        {
            Console.WriteLine("Testing ProgressSpinner...");
            
            using (new ProgressSpinner("Downloading"))
            {
                Thread.Sleep(500);
            }
            
            Console.WriteLine("✓ ProgressSpinner tests completed");
        }

        // Sample 5: GlobalOption tests
        static void TestGlobalOption()
        {
            Console.WriteLine("Testing GlobalOption...");
            
            // Simple flag option (no value expected)
            var verboseOption = new GlobalOption(
                "verbose",
                "Enable verbose logging",
                () => Console.WriteLine("Verbose mode enabled"),
                expectsValue: false,
                "v");
            
            // Option that expects a value
            var configOption = new GlobalOption(
                "config",
                "Path to configuration file",
                (configPath) => 
                {
                    Environment.SetEnvironmentVariable("CONFIG_FILE", configPath);
                    Console.WriteLine($"Using config file: {configPath}");
                },
                expectsValue: true,
                "c", "cfg");
            
            // Debug level option with value
            var debugOption = new GlobalOption(
                "debug-level",
                "Set debug level (1-5)",
                (level) => 
                {
                    if (int.TryParse(level, out int debugLevel))
                        Environment.SetEnvironmentVariable("DEBUG_LEVEL", level);
                    else
                        Console.WriteLine($"Invalid debug level: {level}");
                },
                expectsValue: true,
                "d");
            
            Console.WriteLine("✓ GlobalOption tests completed");
        }

        // Sample 6: RootCommandBuilder basic test
        static async Task TestRootCommandBuilder()
        {
            Console.WriteLine("Testing RootCommandBuilder...");
            
            // Create root command builder
            var builder = new RootCommandBuilder("Test CLI Application");
            
            // Configure features
            builder.EnableHelp = true;
            builder.EnableTypoCorrections = true;
            builder.EnableParameterExplanation = true;
            
            // Add banner
            builder.BannerAction = () =>
            {
                Console.WriteLine("══════════════════════════");
                Console.WriteLine("║  Test Application v1.0   ║");
                Console.WriteLine("══════════════════════════");
            };
            
            // Add global options
            builder.AddGlobalOption(
                "verbose",
                "Enable verbose output",
                () => Environment.SetEnvironmentVariable("VERBOSE", "1"),
                "v");
            
            // Build parser (but don't invoke with real args)
            var parser = builder.Build();
            
            Console.WriteLine("✓ RootCommandBuilder tests completed");
        }

        // Sample 7: Complete Application test
        static async Task<int> TestCompleteApplication(string[] args)
        {
            Console.WriteLine("Testing Complete Application pattern...");
            
            var builder = new RootCommandBuilder("File Manager Tool")
            {
                EnableHelp = true,
                EnableTypoCorrections = true,
                EnableCaseSensitivity = false,
                EnableParameterExplanation = true
            };

            // Add banner
            builder.BannerAction = () =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(@"
╔═══════════════════════════════════╗
║      File Manager Tool v1.0       ║
║   Professional File Operations    ║
╚═══════════════════════════════════╝
");
                Console.ResetColor();
            };
            
            // Add global options
            builder.AddGlobalOption(
                "verbose",
                "Enable detailed logging",
                () => Environment.SetEnvironmentVariable("VERBOSE", "1"),
                "v");
            
            // Load commands from assembly (but don't add TestBed commands)
            // builder.AddAssembly(Assembly.GetExecutingAssembly());
            
            // Build parser (but don't invoke with real args to avoid conflicts)
            var parser = builder.Build();
            
            Console.WriteLine("✓ Complete Application pattern tested");
            return 0;
        }
    }

    // Sample command classes from the XML documentation examples
    [Command("greet", "Greet a person")]
    public class GreetCommands
    {
        [Command("hello", "Say hello to someone")]
        public void Hello(
            [Option("Person's name", "n")] string name,
            [Option("Custom greeting")] string greeting = "Hello")
        {
            Console.WriteLine($"{greeting}, {name}!");
        }
    }

    [Command("process", "Process data files", "proc", "run")]
    public class ProcessCommands
    {
        [Command("start", "Start processing")]
        public void Start(
            [Option("Input file path", "i")] string inputFile,
            [Option("Output directory")] string outputDir = "output")
        {
            Console.WriteLine($"Processing {inputFile} to {outputDir}");
        }
    }

    public class DataCommands
    {
        [Command("import", "Import data from various sources")]
        public static void Import(
            [Option("Input file path", "i")] string inputFile,
            [Option("Data format: csv, json, xml")] string format = "csv",
            [Option("Batch size for processing")] int batchSize = 1000,
            [Option("Skip validation")] bool skipValidation = false)
        {
            Console.WriteLine($"Importing from: {inputFile}");
            Console.WriteLine($"Format: {format}");
            Console.WriteLine($"Batch size: {batchSize}");
            Console.WriteLine($"Skip validation: {skipValidation}");
        }
    }

    public enum LogLevel { Debug, Info, Warning, Error }
    public enum OutputFormat { Json, Xml, Csv }

    public class ConfigCommands
    {
        [Command("setup", "Configure application settings")]
        public static void Setup(
            [Option("Minimum log level")] LogLevel logLevel = LogLevel.Info,
            [Option("Output format for reports")] OutputFormat format = OutputFormat.Json,
            [Option("Enable verbose output")] bool verbose = false)
        {
            Console.WriteLine($"Log Level: {logLevel}");
            Console.WriteLine($"Output Format: {format}");
            Console.WriteLine($"Verbose: {verbose}");
        }
    }

    public class BackupCommands
    {
        [Command("backup", "Backup files to destination")]
        public static void Backup(
            [Option("Source directory", "s")] string source,
            [Option("Destination directory", "d")] string destination)
        {
            using (var spinner = new ConsoleSpinner("Preparing backup"))
            {
                // Validation
                if (!Directory.Exists(source))
                    throw new DirectoryNotFoundException($"Source not found: {source}");
                
                Directory.CreateDirectory(destination);
                Thread.Sleep(1000); // Simulate work
            }
            
            Console.WriteLine("✓ Preparation complete");
            
            using (var spinner = new ConsoleSpinner("Copying files"))
            {
                // Simulate file copy
                Thread.Sleep(3000);
            }
            
            Console.WriteLine("✓ Backup completed!");
        }
    }

    public class DownloadCommands
    {
        [Command("download", "Download files from URL")]
        public static async Task Download(
            [Option("File URL")] string url,
            [Option("Output path")] string output)
        {
            using (new ProgressSpinner("Downloading"))
            {
                // Simulate download
                await Task.Delay(3000);
            }
            
            Console.WriteLine($"✓ Downloaded to: {output}");
        }
    }

    public class DeployCommands
    {
        [Command("deploy", "Deploy application")]
        public static async Task<int> Deploy(
            [Option("Environment")] string environment,
            [Option("Version")] string version)
        {
            Console.WriteLine($"Deploying v{version} to {environment}\n");
            
            using (new ProgressSpinner("Building application"))
            {
                await Task.Delay(2000);
            }
            Console.WriteLine("✓ Build complete\n");
            
            using (new ProgressSpinner("Running tests"))
            {
                await Task.Delay(1500);
            }
            Console.WriteLine("✓ Tests passed\n");
            
            using (var spinner = new ProgressSpinner("Uploading artifacts"))
            {
                await Task.Delay(1000);
                spinner.UpdateMessage("Finalizing upload");
                await Task.Delay(1500);
            }
            Console.WriteLine("✓ Upload complete\n");
            
            Console.WriteLine("✓ Deployment successful!");
            return 0;
        }
    }

    [Command("file", "File operations")]
    public class FileCommands
    {
        [Command("copy", "Copy files")]
        public async Task<int> Copy(
            [Option("Source file path", "s")] string source,
            [Option("Destination path", "d")] string destination,
            [Option("Overwrite existing files")] bool overwrite = false)
        {
            try
            {
                using (new ConsoleSpinner("Validating paths"))
                {
                    if (!File.Exists(source))
                        throw new FileNotFoundException($"Source file not found: {source}");
                    
                    var destDir = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                        Directory.CreateDirectory(destDir);
                    
                    await Task.Delay(500);
                }
                Console.WriteLine("✓ Validation complete\n");
                
                using (new ProgressSpinner("Copying file"))
                {
                    File.Copy(source, destination, overwrite);
                    await Task.Delay(1000); // Simulate copy time
                }
                
                Console.WriteLine($"\n✓ File copied successfully!");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n✗ Copy failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }
        
        [Command("list", "List files in directory")]
        public void List(
            [Option("Directory path")] string directory = ".",
            [Option("Include hidden files")] bool includeHidden = false)
        {
            Console.WriteLine($"Files in: {Path.GetFullPath(directory)}\n");
            
            var searchOption = includeHidden 
                ? SearchOption.AllDirectories 
                : SearchOption.TopDirectoryOnly;
                
            var files = Directory.GetFiles(directory, "*", searchOption);
            
            if (files.Length == 0)
            {
                Console.WriteLine("No files found");
                return;
            }
            
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                Console.WriteLine($"  📄 {info.Name}");
                Console.WriteLine($"     Size: {info.Length} bytes");
                Console.WriteLine($"     Modified: {info.LastWriteTime}");
                Console.WriteLine();
            }
        }
    }

    public class UtilityCommands
    {
        [Command("version", "Show version information", "ver", "v")]
        public static void ShowVersion()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            Console.WriteLine($"File Manager Tool v{version}");
        }
        
        [Command("help", "Show help information", "h", "?")]
        public static void ShowHelp()
        {
            Console.WriteLine("File Manager Tool - Professional File Operations");
            Console.WriteLine();
            Console.WriteLine("Usage: filemgr [global-options] [command] [options]");
            Console.WriteLine();
            Console.WriteLine("Global Options:");
            Console.WriteLine("  --verbose, -v    Enable verbose output");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  file copy        Copy files");
            Console.WriteLine("  file list        List files in directory");
            Console.WriteLine("  version          Show version");
            Console.WriteLine();
            Console.WriteLine("Use 'filemgr [command] --help' for command-specific help");
        }
    }
}
