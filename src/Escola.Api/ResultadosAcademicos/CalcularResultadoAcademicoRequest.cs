using System.ComponentModel.DataAnnotations;

namespace Escola.Api.ResultadosAcademicos;

internal sealed class CalcularResultadoAcademicoRequest
{
    [Required]
    [Range(typeof(decimal), "0", "10")]
    public decimal? Nota1 { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "10")]
    public decimal? Nota2 { get; init; }
}
