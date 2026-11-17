using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Escola.Api.ResultadosAcademicos;

// O ASP.NET Core preenche as notas ao desserializar o corpo JSON.
[UsedImplicitly(ImplicitUseKindFlags.Assign, ImplicitUseTargetFlags.Members)]
internal sealed class CalcularResultadoAcademicoRequest
{
    [Required]
    [Range(typeof(decimal), "0", "10")]
    public decimal? Nota1 { get; init; }

    [Required]
    [Range(typeof(decimal), "0", "10")]
    public decimal? Nota2 { get; init; }
}
