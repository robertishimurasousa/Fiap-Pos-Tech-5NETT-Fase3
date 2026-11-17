using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Allure.Net.Commons.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Escola.IntegrationTests;

[AllureParentSuite("Testes HTTP")]
[AllureSuite("Autenticação JWT")]
public sealed class AutenticacaoJwtHttpTests(EscolaApiFactory factory) : IClassFixture<EscolaApiFactory>
{
    private const string RotaCalculo = "/resultados-academicos/calcular";
    private const string RotaToken = "/auth/token";

    [Theory]
    [InlineData(null)]
    [InlineData("token-invalido")]
    public async Task Calculo_rejeita_requisicao_sem_token_valido(string? token)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = token is null ? null : new("Bearer", token);

        using var response = await client.PostAsJsonAsync(RotaCalculo, new { nota1 = 6, nota2 = 8 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(response.Headers.WwwAuthenticate, challenge => challenge.Scheme == "Bearer");
    }

    [Theory]
    [InlineData("escola-api", "escola-calculo", -5, false)]
    [InlineData("emissor-incorreto", "escola-calculo", 5, false)]
    [InlineData("escola-api", "audiencia-incorreta", 5, false)]
    [InlineData("escola-api", "escola-calculo", 5, true)]
    public async Task Calculo_rejeita_jwt_com_validacao_invalida(
        string emissor, string audiencia, int expiraEmMinutos, bool assinaturaDiferente)
    {
        var chave = assinaturaDiferente
            ? Convert.ToHexString(RandomNumberGenerator.GetBytes(32))
            : factory.ChaveAssinatura;
        var agora = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: emissor,
            audience: audiencia,
            notBefore: agora.AddMinutes(-20),
            expires: agora.AddMinutes(expiraEmMinutos),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), SecurityAlgorithms.HmacSha256));
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));

        using var response = await client.PostAsJsonAsync(RotaCalculo, new { nota1 = 6, nota2 = 8 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Codigo_correto_emite_token_e_permite_calcular()
    {
        using var client = factory.CreateClient();
        await factory.AutenticarAsync(client);

        using var response = await client.PostAsJsonAsync(RotaCalculo, new { nota1 = 6, nota2 = 8 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Codigo_incorreto_retorna_401_sem_token()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(RotaToken, new { codigo = "codigo-incorreto" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Emissao_local_esta_ausente_em_production()
    {
        using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();
        using var tokenResponse = await client.PostAsJsonAsync(RotaToken, new { codigo = factory.CodigoAcesso });
        using var calculoResponse = await client.PostAsJsonAsync(RotaCalculo, new { nota1 = 6, nota2 = 8 });

        Assert.Equal(HttpStatusCode.NotFound, tokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, calculoResponse.StatusCode);
    }

    [Fact]
    public async Task OpenApi_descreve_bearer_para_calculo_e_emissao_publica()
    {
        using var client = factory.CreateClient();
        using var json = JsonDocument.Parse(await client.GetStringAsync("/openapi/v1.json"));
        var documento = json.RootElement;
        var esquema = documento.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", esquema.GetProperty("type").GetString());
        Assert.Equal("bearer", esquema.GetProperty("scheme").GetString());

        var rotas = documento.GetProperty("paths");
        var calculo = rotas.GetProperty(RotaCalculo).GetProperty("post");
        Assert.True(calculo.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.True(calculo.GetProperty("responses").TryGetProperty("401", out _));
        var emissao = rotas.GetProperty(RotaToken).GetProperty("post");
        Assert.False(emissao.TryGetProperty("security", out _));
    }
}
