namespace Escola.Api.ResultadosAcademicos;

public sealed record ResultadoAcademico(decimal Media, SituacaoAcademica Situacao);

public enum SituacaoAcademica
{
    Aprovado,
    Reprovado,
}
