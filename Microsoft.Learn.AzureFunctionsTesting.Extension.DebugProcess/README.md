# Azure Functions Integration Testing Framework - Debugging

Since these are integration tests, they test the code like a client would by making requests and inspecting the result. However, it is often useful to be able to 
see what the server is doing and step through the code. Since the tests and the functions host run in two (or three) different processes, that can be tricky.
The solution is the `Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess` package.

## Setup

### Test Project

Add a reference to this package from your test project. In your TestStartup, call `builder.DebugIntoFunctions()`, which will cause Visual Studio
to automatically attach to the `func.exe` process when the tests are being debugged.

### Function App

Since the code has to wait until the process starts in order to attach to it, there can be timing issues where you may not be able to debug the very
earliest lines of code in your app. To solve that, add a reference to `Microsoft.Learn.AzureFunctionsTesting.Extension.DebugProcess.Core` to your function app
(not tests app) and call `DebugHelper.WaitForDebuggerToAttach()` as the very first line of code (in your Startup.cs or Program.cs)

## Usage

When you *Debug* your tests, VS will automatically attach to the function process as well (including the worker process for Isolated functions) so you can 
set breakpoints and step through both the test code and the functions being tested.

When you *Run* your tests, and when the code is directly outside of tests (including when deployed to Azure), the `DebugHelper.WaitForDebuggerToAttach()` is a no-op and wont cause any harm.
