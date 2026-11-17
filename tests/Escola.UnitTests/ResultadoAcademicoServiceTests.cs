using Allure.Net.Commons.Attributes;
using Escola.Api.ResultadosAcademicos;

namespace Escola.UnitTests;

[AllureParentSuite("Testes unitários")]
[AllureSuite("Regra de cálculo acadêmico")]
public sealed class ResultadoAcademicoServiceTests
{
    private readonly ResultadoAcademicoService _servico = new();

    public static TheoryData<decimal, decimal, decimal, SituacaoAcademica> NotasValidas => new()
    {
        { 8m, 9m, 8.5m, SituacaoAcademica.Aprovado },
        { 6m, 8m, 7m, SituacaoAcademica.Aprovado },
        { 6m, 7m, 6.5m, SituacaoAcademica.Reprovado },
        { 6.99m, 7m, 6.995m, SituacaoAcademica.Reprovado },
        { 0m, 10m, 5m, SituacaoAcademica.Reprovado },
        { 10m, 0m, 5m, SituacaoAcademica.Reprovado },
    };

    public static TheoryData<decimal?, decimal?, string> NotasAusentes => new()
    {
        { null, 8m, "nota1" },
        { 8m, null, "nota2" },
    };

    public static TheoryData<decimal, decimal, string> NotasForaDaFaixa => new()
    {
        { -1m, 8m, "nota1" },
        { 11m, 8m, "nota1" },
        { 8m, -1m, "nota2" },
        { 8m, 11m, "nota2" },
    };

    [Theory]
    [MemberData(nameof(NotasValidas))]
    public void Calcular_retorna_media_e_situacao_para_notas_validas(
        decimal nota1,
        decimal nota2,
        decimal mediaEsperada,
        SituacaoAcademica situacaoEsperada)
    {
        var resultado = _servico.Calcular(nota1, nota2);

        Assert.Equal(mediaEsperada, resultado.Media);
        Assert.Equal(situacaoEsperada, resultado.Situacao);
    }

    [Theory]
    [MemberData(nameof(NotasAusentes))]
    public void Calcular_rejeita_nota_ausente(decimal? nota1, decimal? nota2, string parametro)
    {
        Assert.Throws<ArgumentNullException>(parametro, () => _servico.Calcular(nota1, nota2));
    }

    [Theory]
    [MemberData(nameof(NotasForaDaFaixa))]
    public void Calcular_rejeita_nota_fora_da_faixa(decimal nota1, decimal nota2, string parametro)
    {
        Assert.Throws<ArgumentOutOfRangeException>(parametro, () => _servico.Calcular(nota1, nota2));
    }
}
