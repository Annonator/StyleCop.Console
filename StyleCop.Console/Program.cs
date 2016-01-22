using System;
using System.IO;
using System.Reflection;

namespace StyleCop.Console
{
    public class Program
    {
        private static int _encounteredViolations;

        public static int Main(string[] args)
        {
            try
            {
                var arguments = new RunnerArguments(args);

                if (arguments.Help)
                {
                    ShowHelp();
                    return (int)ExitCode.Failed;
                }

                if (string.IsNullOrWhiteSpace(arguments.ProjectPath) || !Directory.Exists(arguments.ProjectPath))
                {
                    ShowHelp();
                    System.Console.WriteLine("");
                    System.Console.WriteLine("ERROR: Invalid or no path specified \"{0}\"!", arguments.ProjectPath);
                    return (int)ExitCode.Failed;
                }

                string settings = !string.IsNullOrWhiteSpace(arguments.SettingsLocation) ? arguments.SettingsLocation : Path.Combine(Assembly.GetExecutingAssembly().Location, "Settings.StyleCop");

                SearchOption searchOption = arguments.NotRecursive ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

                string projectPath = arguments.ProjectPath;
                string outputPath = arguments.OutputPath;

                return ProcessFolder(settings, projectPath, outputPath, searchOption);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("An unhandled exception occured: {0}", ex);
                return (int)ExitCode.Failed;
            }
        }

        private static int ProcessFolder(string settings, string projectPath, string outputPath, SearchOption searchOption)
        {
            var console = new StyleCopConsole(settings, false, outputPath, null, true);
            var project = new CodeProject(0, projectPath, new Configuration(null));

            foreach (string file in Directory.EnumerateFiles(projectPath, "*.cs", searchOption))
            {
                //TODO: This is pretty hacky. Have to figure out a better way to exclude packages and/or make this configurable.
                if (file.Contains("\\packages\\"))
                {
                    continue;
                }

                console.Core.Environment.AddSourceCode(project, file, null);
            }

            console.OutputGenerated += OnOutputGenerated;
            console.ViolationEncountered += OnViolationEncountered;
            console.Start(new[]
                              {
                                  project
                              }, true);
            console.OutputGenerated -= OnOutputGenerated;
            console.ViolationEncountered -= OnViolationEncountered;

            return _encounteredViolations > 0 ? (int)ExitCode.Failed : (int)ExitCode.Passed;
        }

        private static void ShowHelp()
        {
            System.Console.WriteLine("-------------------");
            System.Console.WriteLine("| StyleCop Runner |");
            System.Console.WriteLine("-------------------");
            System.Console.WriteLine("");
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("  -project-path <path> or -p <path> ....... The path to analyze cs-files in");
            System.Console.WriteLine("  -output-path <path> or -o <path> ....... The output path of Validation.xml");
            System.Console.WriteLine("  -settings-location <path> or -s <path> .. The path to 'Settings.StyleCop'");
            System.Console.WriteLine("  -not-recursively or -n .................. Do not process path recursively");
            System.Console.WriteLine("  -help or -? ............................. Show this screen");
        }

        private static void OnOutputGenerated(object sender, OutputEventArgs e)
        {
            System.Console.WriteLine(e.Output);
        }

        private static void OnViolationEncountered(object sender, ViolationEventArgs e)
        {
            _encounteredViolations++;
            WriteLineViolationMessage(string.Format("  Line {0}: {1} ({2})", e.LineNumber, e.Message, e.Violation.Rule.CheckId));
        }

        private static void WriteLineViolationMessage(string message)
        {
            System.Console.ForegroundColor = ConsoleColor.DarkRed;
            System.Console.WriteLine(message);
            System.Console.ResetColor();
        }
    }
}