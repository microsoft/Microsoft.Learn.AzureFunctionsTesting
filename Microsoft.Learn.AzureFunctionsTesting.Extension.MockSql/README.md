# Azure Functions Integration Testing Framework - Mocking SqlServer

If your Azure Functions app uses SqlServer, you can use this package to make mocking easy with no code changes required to your function app code.

## Prerequisites

- [LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15) installed and running

## Setup

In your TestStartup, call `builder.UseSqlServer()`. If your code connects to multiple SqlServers, you can use the overload that takes a `name` and call `.UseSqlServer()` multiple times, once for each database you use.

You can use either `.sql` files or DacPac files to seed your database. `.UseSqlServer()` returns an object with the connection string of the local DB, which you can use to configure your function app however you would normally pass in your connection string:

	var sqlConnectionString = builder.UseSqlServer().WithSqlScript(@"Helpers\setup.sql").ConnectionString;

	builder.ConfigureEnvironmentVariables(env =>
	{
		env.Add("SqlConnectionString", sqlInfo.ConnectionString);
	});

## Usage

In your tests, you can call `fixture.GetSqlDatabase()` (optionally passing in a `name` if you configured multiple DBs) which will return a `LocalDatabase` object which contains the connection string of the local DB.

You can use this connection string to create a `SqlConnection` object and execute your SQL commands as needed.

A convenient approach is to wrap your logic in an extension method like this:

    public static async Task AddUserToDatabase<T>(this FunctionFixture<T> fixture, string userId, string name) where T : IFunctionTestStartup
    {
        var db = fixture.GetSqlDatabase();
        if (db == null) return;

        var parameters = new List<SqlParameter>
        {
            new SqlParameter("@userId", userId),
            new SqlParameter("@name", name)
        }.ToArray();

        using var connection = new SqlConnection(db.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandType = System.Data.CommandType.StoredProcedure;
        command.CommandText = "CreateUser";
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

Then in your tests, you can do stuff like:

	await fixture.AddUserToDatabase("123", "Test User");

Which makes per-test data setup straightforward.
