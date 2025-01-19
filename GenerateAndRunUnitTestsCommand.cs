using System;
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

		private GenerateAndRunUnitTestsCommand(Package package)
		{
			_package = package ?? throw new ArgumentNullException(nameof(package));

			var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			commandService?.AddCommand(new MenuCommand(Execute, new CommandID(CommandSet, CommandId)));
		}

		public static GenerateAndRunUnitTestsCommand Instance { get; private set; }
		private IServiceProvider ServiceProvider => _package;

		public static void Initialize(Package package) => Instance = new GenerateAndRunUnitTestsCommand(package);

		private void Execute(object sender, EventArgs e)
		{
			var dte = ServiceProvider.GetService(typeof(DTE)) as DTE;
			if (dte?.ActiveDocument?.Selection is TextSelection textSelection)
			{
				var caretText = textSelection.Text.Trim();
				Log($"Caret is on: {caretText}");

				if (string.IsNullOrWhiteSpace(caretText) || caretText != "test")
				{
					Log("Caret is not on a valid `test` annotation.");
					throw new InvalidOperationException("Caret is not on a valid `test` annotation.");
				}
			}
			else
			{
				Log("Unable to retrieve text selection.");
				throw new InvalidOperationException("Unable to retrieve text selection.");
			}



		private void GenerateAndRunTests(string code)
		{
			var methodsToTest = ExtractMethodsWithTestAnnotation(code);

			if (methodsToTest.Length == 0)
			{
				Log("No methods found with the `test` annotation.");
				throw new InvalidOperationException("No methods found with the `test` annotation.");
			}

			foreach (var method in methodsToTest)
			{
				Log($"Found method to test: {method}");
				var testCode = GenerateUnitTestCode(method);
				WriteTestToFile(testCode);
				RunTests();
			}
		}


		private static string[] ExtractMethodsWithTestAnnotation(string code)
		{
			Log("Extracting methods annotated with `test`...");
			var methodRegex = new Regex(@"^\s*test\s*$\s*(public|private|protected|internal)\s+\w+\s+\w+\(.*?\)\s*{", RegexOptions.Multiline);

			var matches = methodRegex.Matches(code);
			var methods = matches.Cast<Match>().Select(m => m.Value).ToArray();

			Log($"{methods.Length} method(s) found with `test` annotation.");
			return methods;
		}

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


		private static void WriteTestToFile(string testCode)
		{
			var testFilePath = Path.Combine(Directory.GetCurrentDirectory(), "GeneratedTests.cs");
			File.WriteAllText(testFilePath, testCode);

			Log($"Test code written to file: {testFilePath}");
		}

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

		private static void Log(string message)
		{
			var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExtensionLog.txt");
			File.AppendAllText(logFilePath, $"{DateTime.Now}: {message}{Environment.NewLine}");
		}

	}
}


