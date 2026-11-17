using System.Net;
using System.Text;
using System.Text.Json;
using Allure.Net.Commons;
using Allure.Net.Commons.Attributes;

namespace Escola.IntegrationTests;

[AllureParentSuite("Testes HTTP")]
[AllureSuite("Resultado acadêmico")]
public sealed class ResultadosAcademicosHttpTests(EscolaApiFactory factory) : IClassFixture<EscolaApiFactory>
{
    private const string Rota = "/resultados-academicos/calcular";

    public static TheoryData<string, decimal, string> ResultadosValidos => new()
    {
        { """{"nota1":8,"nota2":9}""", 8.5m, "Aprovado" },
        { """{"nota1":6,"nota2":8}""", 7m, "Aprovado" },
        { """{"nota1":6.99,"nota2":7.00}""", 6.995m, "Reprovado" },
        { """{"nota1":0,"nota2":10}""", 5m, "Reprovado" },
    };

    [Theory]
    [MemberData(nameof(ResultadosValidos))]
    public async Task Calcular_retorna_200_com_media_e_situacao(
        string entrada,
        decimal mediaEsperada,
        string situacaoEsperada)
    {
        using var cliente = factory.CreateClient();
        await AllureApi.Step("Autenticar para executar o cálculo", () => factory.AutenticarAsync(cliente));
        AllureApi.AddAttachment("Entrada do cálculo", "application/json", Encoding.UTF8.GetBytes(entrada), ".json");
        using var corpo = new StringContent(entrada, Encoding.UTF8, "application/json");
        using var resposta = await AllureApi.Step("Enviar POST de cálculo", () => cliente.PostAsync(Rota, corpo));
        var conteudo = await resposta.Content.ReadAsStringAsync();
        AllureApi.AddAttachment("Resposta do cálculo", "application/json", Encoding.UTF8.GetBytes(conteudo), ".json");
        var statusCode = resposta.StatusCode;
        var contentType = resposta.Content.Headers.ContentType?.MediaType;
        AllureApi.AddTestParameter("HTTP recebido", (int)statusCode);

        AllureApi.Step("Conferir HTTP 200, média e situação", () =>
        {
            Assert.Equal(HttpStatusCode.OK, statusCode);
            Assert.Equal("application/json", contentType);
            using var json = JsonDocument.Parse(conteudo);
            Assert.Equal(mediaEsperada, json.RootElement.GetProperty("media").GetDecimal());
            Assert.Equal(situacaoEsperada, json.RootElement.GetProperty("situacao").GetString());
        });
    }

    [Theory]
    [InlineData("""{"nota2":8}""", "nota1")]
    [InlineData("""{"nota1":8}""", "nota2")]
    [InlineData("""{"nota1":null,"nota2":8}""", "nota1")]
    [InlineData("""{"nota1":-1,"nota2":8}""", "nota1")]
    [InlineData("""{"nota1":8,"nota2":11}""", "nota2")]
    [InlineData("""{"nota1":-0.01,"nota2":8}""", "nota1")]
    [InlineData("""{"nota1":8,"nota2":10.01}""", "nota2")]
    public async Task Calcular_retorna_400_com_problem_details_para_nota_invalida(
        string entrada,
        string campoInvalido)
    {
        using var cliente = factory.CreateClient();
        await AllureApi.Step("Autenticar para executar o cálculo", () => factory.AutenticarAsync(cliente));
        AllureApi.AddAttachment("Entrada inválida", "application/json", Encoding.UTF8.GetBytes(entrada), ".json");
        using var corpo = new StringContent(entrada, Encoding.UTF8, "application/json");
        using var resposta = await AllureApi.Step("Enviar POST de cálculo", () => cliente.PostAsync(Rota, corpo));
        var conteudo = await resposta.Content.ReadAsStringAsync();
        AllureApi.AddAttachment("Problem Details", "application/json", Encoding.UTF8.GetBytes(conteudo), ".json");
        var statusCode = resposta.StatusCode;
        var contentType = resposta.Content.Headers.ContentType?.MediaType;
        AllureApi.AddTestParameter("HTTP recebido", (int)statusCode);

        AllureApi.Step("Conferir HTTP 400 e campo inválido", () =>
        {
            Assert.Equal(HttpStatusCode.BadRequest, statusCode);
            Assert.Equal("application/problem+json", contentType);
            using var json = JsonDocument.Parse(conteudo);
            Assert.Equal(400, json.RootElement.GetProperty("status").GetInt32());
            Assert.Contains(
                json.RootElement.GetProperty("errors").EnumerateObject(),
                erro => string.Equals(erro.Name, campoInvalido, StringComparison.OrdinalIgnoreCase));
        });
    }
}
