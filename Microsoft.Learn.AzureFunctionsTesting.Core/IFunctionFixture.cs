namespace Microsoft.Learn.AzureFunctionsTesting.Core
{
    public interface IFunctionFixture
    {
        T? GetPlugin<T>(string name) where T : IFunctionTestPlugin;
    }
}
