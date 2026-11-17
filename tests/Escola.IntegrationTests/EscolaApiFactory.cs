using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Escola.IntegrationTests;

// O xUnit instancia a factory para os testes que usam IClassFixture.
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public sealed class EscolaApiFactory : WebApplicationFactory<Program>
{
    internal string ChaveAssinatura { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    internal string CodigoAcesso { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ChaveAssinatura"] = ChaveAssinatura,
                ["Jwt:CodigoAcesso"] = CodigoAcesso,
            }));
    }

    internal async Task AutenticarAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/auth/token", new { codigo = CodigoAcesso });
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", json.RootElement.GetProperty("accessToken").GetString());
    }
}
