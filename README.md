# TDDUnitExtension

# A Visual Studio extension I designed to streamline the process of test-driven development (TDD). This extension allows you to quickly generate and run unit tests for methods annotated with the keyword test.

Features
Detects methods in C# files annotated with the keyword test.
Automatically generates unit tests for annotated methods using the xUnit framework.
Saves generated unit tests in a file named GeneratedTests.cs.
Executes the tests using the dotnet test command and logs the results.
Provides detailed logs to help with debugging.
How It Works

# 1. Annotate Methods:

Place the word test on a line by itself directly above the method you want to generate tests for.

# 2. Run the Extension:

Trigger the extension using the menu or a keyboard shortcut in Visual Studio.

# 3. Generated Output:

The extension generates unit tests for the annotated methods:

# 4. Run the Tests:

The extension executes dotnet test to run the generated unit tests.
Test results are displayed in the Visual Studio Output window.

# 5. Logs:

All activity is logged in a file called ExtensionLog.txt, located in your Documents folder.

