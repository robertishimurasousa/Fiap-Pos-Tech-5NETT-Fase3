namespace Escola.Api.ResultadosAcademicos;

public sealed class ResultadoAcademicoService
{
    public ResultadoAcademico Calcular(decimal? nota1, decimal? nota2)
    {
        var primeiraNota = ValidarNota(nota1, nameof(nota1));
        var segundaNota = ValidarNota(nota2, nameof(nota2));

        var media = (primeiraNota + segundaNota) / 2m;
        var situacao = media >= 7m
            ? SituacaoAcademica.Aprovado
            : SituacaoAcademica.Reprovado;

        return new ResultadoAcademico(media, situacao);
    }

    private static decimal ValidarNota(decimal? nota, string parametro)
    {
        if (nota is not { } valor)
        {
            throw new ArgumentNullException(parametro, "A nota é obrigatória.");
        }

        if (valor is < 0m or > 10m)
        {
            throw new ArgumentOutOfRangeException(parametro, valor, "A nota deve estar entre 0 e 10.");
        }

        return valor;
    }
}
