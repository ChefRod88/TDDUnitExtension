uusing System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio.Shell;

namespace TDDUnitTestExtension
{
    internal sealed class GenerateAndRunUnitTestsCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("your-guid-here");
        private readonly Package _package;

        // Constructor to initialize the command with the given package
        private GenerateAndRunUnitTestsCommand(Package package)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));

            // Adding the command to the command service
            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            commandService?.AddCommand(new MenuCommand(Execute, new CommandID(CommandSet, CommandId)));
        }

        // Singleton instance of the command
        public static GenerateAndRunUnitTestsCommand Instance { get; private set; }

        // Provides access to the service provider
        private IServiceProvider ServiceProvider => _package;

        // Initializes the command
        public static void Initialize(Package package) => Instance = new GenerateAndRunUnitTestsCommand(package);

        // Executes the command when triggered
        private void Execute(object sender, EventArgs e)
        {
            var dte = ServiceProvider.GetService(typeof(DTE)) as DTE;
            if (dte?.ActiveDocument?.Selection is TextSelection textSelection)
            {
                var caretText = textSelection.Text.Trim();
                Log($"Caret is on: {caretText}");

                // Checks if the caret is on a valid `[Test]` annotation
                if (string.IsNullOrWhiteSpace(caretText) || caretText != "[Test]")
                {
                    Log("Caret is not on a valid `[Test]` annotation.");
                    throw new InvalidOperationException("Caret is not on a valid `[Test]` annotation.");
                }

                // Retrieves the code from the active document
                var code = dte.ActiveDocument?.GetText();
                if (code == null)
                {
                    Log("Unable to retrieve code.");
                    throw new InvalidOperationException("Unable to retrieve code.");
                }

                // Generates and runs the tests for the annotated methods
                GenerateAndRunTests(code);
            }
            else
            {
                Log("Unable to retrieve text selection.");
                throw new InvalidOperationException("Unable to retrieve text selection.");
            }
        }

        // Generates and runs unit tests for methods with the `[Test]` annotation
        private void GenerateAndRunTests(string code)
        {
            var methodsToTest = ExtractMethodsWithTestAnnotation(code);

            if (methodsToTest.Length == 0)
            {
                Log("No methods found with the `[Test]` annotation.");
                throw new InvalidOperationException("No methods found with the `[Test]` annotation.");
            }

            foreach (var method in methodsToTest)
            {
                Log($"Found method to test: {method}");
                var testCode = GenerateUnitTestCode(method);
                WriteTestToFile(testCode);
                RunTests();
            }
        }

        // Extracts methods annotated with the `[Test]` attribute from the given code
        private static string[] ExtractMethodsWithTestAnnotation(string code)
        {
            Log("Extracting methods annotated with `[Test]`...");
            var methodRegex = new Regex(@"

\[\s*Test\s*]\s*(public|private|protected|internal)\s+\w+\s+\w+\(.*?\)\s*{", RegexOptions.Multiline);

            var matches = methodRegex.Matches(code);
            var methods = matches.Cast<Match>().Select(m => m.Value).ToArray();

            Log($"{methods.Length} method(s) found with `[Test]` annotation.");
            return methods;
        }

        // Generates unit test code for the given method
        private static string GenerateUnitTestCode(string method)
        {
            Log($"Generating test for method: {method}");
            var methodName = Regex.Match(method, @"\w+\(")?.Value.Trim('(') ?? "UnknownMethod";

            return $@"
using Xunit;

public class {methodName}Tests
{{
    [Fact]
    public void {methodName}_ShouldBehaveAsExpected()
    {{
        // Arrange
        var instance = new YourClassName();

        // Act
        var result = instance.{methodName}();

        // Assert
        Assert.NotNull(result);
    }}
}}";
        }

        // Writes the generated test code to a file
        private static void WriteTestToFile(string testCode)
        {
            var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedTests.cs");
            File.WriteAllText(testFilePath, testCode);

            Log($"Test code written to file: {testFilePath}");
        }

        // Runs the generated tests using `dotnet test`
        private static void RunTests()
        {
            Log("Running tests using `dotnet test`...");
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "test",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Log("Test execution completed.");
            Log("Test Output:");
            Log(output);
        }

        // Logs messages to a file
        private static void Log(string message)
        {
            var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExtensionLog.txt");
            File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
    }
}



