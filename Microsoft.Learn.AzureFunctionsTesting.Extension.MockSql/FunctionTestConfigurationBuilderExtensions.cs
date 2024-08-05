using Microsoft.Learn.AzureFunctionsTesting.Core;

namespace Microsoft.Learn.AzureFunctionsTesting.Extension.MockSql
{
    public static class FunctionTestConfigurationBuilderExtensions
    {
        public static SqlConfigBuilder UseSqlServer(this IFunctionTestConfigurationBuilder builder)
        {
            return builder.UseSqlServer(MockSqlPlugin.DefaultName);
        }

        public static SqlConfigBuilder UseSqlServer(this IFunctionTestConfigurationBuilder builder, string name)
        {
            var plugin = new MockSqlPlugin(name);
            builder.RegisterPlugin(plugin, plugin.Name);
            return new SqlConfigBuilder(plugin);
        }
    }

    public class SqlConfigBuilder
    {
        readonly MockSqlPlugin plugin;

        internal SqlConfigBuilder(MockSqlPlugin plugin)
        {
            this.plugin = plugin;
        }

        public SqlConfigBuilder WithDacPac(string dacpacPath)
        {
            plugin.AddDacPac(dacpacPath);
            return this;
        }

        public SqlConfigBuilder WithSqlScript(params string[] sqlInitFiles)
        {
            foreach (var sqlInitFile in sqlInitFiles)
            {
                plugin.AddSqlScript(sqlInitFile);
            }
            return this;
        }

        public string ConnectionString => plugin.Database.ConnectionString;
    }
}
