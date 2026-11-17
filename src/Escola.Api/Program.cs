using System.Text.Json.Serialization;
using Escola.Api.ResultadosAcademicos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

const string rotaCalculo = "/resultados-academicos/calcular";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ResultadoAcademicoService>();
builder.Services.AddOpenApi(options => options.AddSchemaTransformer((schema, context, _) =>
{
    if (context.JsonTypeInfo.Type == typeof(CalcularResultadoAcademicoRequest) && schema.Properties is not null)
    {
        // As propriedades aceitam null na desserialização para que o endpoint possa devolver Problem Details.
        foreach (var property in schema.Properties.Values.OfType<OpenApiSchema>())
        {
            property.Type = JsonSchemaType.Number;
        }
    }

    return Task.CompletedTask;
}));
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter<SituacaoAcademica>(allowIntegerValues: false));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Escola — API de Resultado Acadêmico"));

    if (app.Configuration["Escola:EnderecoLocal"] is { Length: > 0 } enderecoLocal)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
            app.Logger.LogInformation(
                """
                API (POST): {UrlApi}
                Scalar: {UrlScalar}
                OpenAPI: {UrlOpenApi}
                Iniciado em {Inicio:dd/MM/yyyy HH:mm:ss zzz}
                """,
                $"{enderecoLocal}{rotaCalculo}",
                $"{enderecoLocal}/scalar/v1",
                $"{enderecoLocal}/openapi/v1.json",
                DateTimeOffset.Now));
    }
}

app.MapPost(rotaCalculo, CalcularResultadoAcademico)
    .WithName("CalcularResultadoAcademico")
    .WithTags("Resultados acadêmicos")
    .WithSummary("Calcula o resultado acadêmico a partir de duas notas")
    .WithDescription("Informe duas notas de 0 a 10. A média é calculada com pesos iguais, sem arredondamento. A aprovação começa em 7,0.");

app.Run();

static Results<Ok<ResultadoAcademico>, ValidationProblem> CalcularResultadoAcademico(
    CalcularResultadoAcademicoRequest request,
    ResultadoAcademicoService service)
{
    try
    {
        return TypedResults.Ok(service.Calcular(request.Nota1, request.Nota2));
    }
    catch (ArgumentException erro) when (erro.ParamName is "nota1" or "nota2")
    {
        var mensagem = erro is ArgumentNullException
            ? "A nota é obrigatória."
            : "A nota deve estar entre 0 e 10.";

        return TypedResults.ValidationProblem(new Dictionary<string, string[]>
        {
            [erro.ParamName] = [mensagem],
        });
    }
}
