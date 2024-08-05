using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public class TestFramework : XunitTestFramework
    {
        public TestFramework(IMessageSink messageSink) : base(messageSink) { }

        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new TestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
        }

        protected override ITestFrameworkDiscoverer CreateDiscoverer(IAssemblyInfo assemblyInfo)
        {
            return base.CreateDiscoverer(assemblyInfo);
        }
    }
}
