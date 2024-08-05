using Microsoft.Learn.AzureFunctionsTesting.Core;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql
{
    public static class FunctionFixtureExtensions
    {
        public static LocalDatabase? GetSqlDatabase(this IFunctionFixture fixture)
        {
            return fixture.GetSqlDatabase(MockSqlPlugin.DefaultName);
        }

        public static LocalDatabase? GetSqlDatabase(this IFunctionFixture fixture, string name)
        {
            var plugin = fixture.GetPlugin<MockSqlPlugin>(name);
            return plugin?.Database;
        }
    }
}
