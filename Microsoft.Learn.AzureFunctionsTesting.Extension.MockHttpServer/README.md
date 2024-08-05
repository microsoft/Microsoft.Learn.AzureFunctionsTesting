# Azure Functions Integration Testing Framework - Mocking HTTP Dependencies

If your function app makes HTTP requests to downstream services, you can use this package to mock those services in your tests. This allows you to test your function app in isolation without needing to call real services.

## Prerequisites

There are no prerequisites for this package.

## Setup

For simple usage, the easiest way to mock HTTP requests is to use the `builder.UseMockServer(name, handler)` method in your TestStartup and provide the `handler` logic inline:

	var someServer = builder.UseMockServer("someServer", (req, res) =>
	{
		var obj = new { id = "abc", name = "test obj", timestamp = DateTimeOffset.UtcNow };
		res.FromJson(obj);
	});

The `handler` delegate is passed an `HttpListenerRequest` and `MockHttpResponse` object, which you can use to inspect the request and write a response. You can set the response status code, headers, and body as needed. The `res.FromJson(obj)` method is a convenience method to write a JSON responses with a single line.

If you need to mock more complex behavior, you can write a custom class that derives from `HttpServer` and call the `.UseMockServer()` overloads that use this type instead of the `handler` delegate.

	var commentServer = builder.UseMockServer<CommentServer>("commentServer"); // an instance of CommentServer will be created automatically

	var orderServer = builder.UseMockServer<OrderServer>("orderServer", new OrderServer()); // you can pass in an instance of the server if you need to configure it

Every server that you create will be assigned a unique port number, so you can run multiple servers in parallel. The `.UseMockServer()` method returns an object with the URL of the server, which you can use to configure your function app however you would normally pass in your service URL:

	builder.ConfigureEnvironmentVariables(env =>
	{
		env.Add("SomeServiceUrl", someServer.Url);
		env.Add("CommentServiceUrl", commentServer.Url);
		env.Add("OrderServiceUrl", orderServer.Url);
	});

## Usage

Since you passed in the url of the servers to your function app, you dont generally have to do anything else at all. 

However, if you need to control the mock server behavior from your tests, you can use the `fixture.GetHttpServer(name)` method to get an `HttpServer` object that you can use to manipulate the mock server behavior.
