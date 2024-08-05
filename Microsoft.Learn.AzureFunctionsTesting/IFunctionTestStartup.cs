namespace Microsoft.Learn.AzureFunctionsTesting
{
    public interface IFunctionTestStartup
    {
        void Configure(FunctionTestConfigurationBuilder builder);
    }
}
