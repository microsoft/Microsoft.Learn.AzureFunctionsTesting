namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockCosmos
{
    public class CosmosDbInfo
    {
        public CosmosDbInfo(string url, string key)
        {
            this.Url = url;
            this.Key = key;
        }

        public string Url { get; }
        public string Key { get; }
    }
}
