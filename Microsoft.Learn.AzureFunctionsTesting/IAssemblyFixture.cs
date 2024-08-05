using Xunit;

namespace Microsoft.Learn.AzureFunctionsTesting
{
    public interface IAssemblyFixture<T> : IClassFixture<T> where T : class
    {
    }
}
