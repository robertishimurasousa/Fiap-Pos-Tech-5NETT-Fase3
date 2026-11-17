using System.Net;
using System.Net.Http.Json;
using System.Text;
using Allure.Net.Commons.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Escola.IntegrationTests;

[AllureParentSuite("Testes HTTP")]
[AllureSuite("Configuração JWT e inicialização")]
public sealed class ConfiguracaoJwtHttpTests(EscolaApiFactory factory) : IClassFixture<EscolaApiFactory>
{
    public static TheoryData<string, string?, string?> ConfiguracoesInvalidas => new()
    {
        { "Development", null, new string('c', 32) },
        { "Development", " ", new string('c', 32) },
        { "Development", new string('a', 31), new string('c', 32) },
        { "Development", new string('a', 32), null },
        { "Development", new string('a', 32), " " },
        { "Development", new string('a', 32), new string('c', 31) },
        { "Development", new string('a', 32), new string('a', 32) },
        { "Development", new string('a', 32), new string('é', 16) },
        { "Production", new string('a', 31), null },
    };

    [Theory]
    [MemberData(nameof(ConfiguracoesInvalidas))]
    public void Configuracao_invalida_impede_inicializacao(
        string ambiente, string? chave, string? codigo)
    {
        using var application = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(ambiente);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:ChaveAssinatura"] = chave,
                    ["Jwt:CodigoAcesso"] = codigo,
                }));
        });

        var error = Assert.Throws<OptionsValidationException>(() => application.CreateClient());

        Assert.Contains(error.Failures, failure => failure.Contains("Jwt:"));
    }

    [Fact]
    public async Task Chave_utf8_com_32_bytes_e_codigo_com_32_caracteres_permitem_autenticacao()
    {
        var codigo = new string('c', 32);
        using var application = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:ChaveAssinatura"] = new string('é', 16),
                    ["Jwt:CodigoAcesso"] = codigo,
                })));
        using var client = application.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/token", new { codigo });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var token = json.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrEmpty(token));
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        using var calculo = await client.PostAsJsonAsync(
            "/resultados-academicos/calcular", new { nota1 = 6, nota2 = 8 });
        Assert.Equal(HttpStatusCode.OK, calculo.StatusCode);
    }

    [Fact]
    public async Task Production_inicia_sem_codigo_de_emissao_local()
    {
        using var application = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:CodigoAcesso"] = null,
                }));
        });
        using var client = application.CreateClient();
        using var response = await client.PostAsJsonAsync("/auth/token", new { codigo = "qualquer" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"codigo\":null}")]
    public async Task Emissao_sem_codigo_retorna_401(string entrada)
    {
        using var client = factory.CreateClient();
        using var body = new StringContent(entrada, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/auth/token", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    }
}
