using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using System.Text.Json;

namespace Tabatine.Worker.IntegrationTests.Helpers;

public static class OmieApiMockExtensions
{
    public static WireMockServer SetupListarClientes(this WireMockServer server, object responseBody, int statusCode = 200)
    {
        server.Given(
            Request.Create()
                .WithPath("/geral/clientes/")
                .WithHeader("Content-Type", "application/json*")
                .UsingPost()
                .WithBody(new JsonPathMatcher("$.call == 'ListarClientes'"))
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(JsonSerializer.Serialize(responseBody))
        );

        return server;
    }

    public static WireMockServer SetupGenericError(this WireMockServer server, string path, int statusCode, object? errorBody = null)
    {
        server.Given(
            Request.Create()
                .WithPath(path)
                .UsingPost()
        )
        .RespondWith(
            Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(errorBody != null ? JsonSerializer.Serialize(errorBody) : "{}")
        );

        return server;
    }
}
