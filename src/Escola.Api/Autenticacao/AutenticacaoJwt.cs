using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Escola.Api.Autenticacao;

internal static class AutenticacaoJwt
{
    private const string Emissor = "escola-api";
    private const string Audiencia = "escola-calculo";
    private const int ValidadeEmSegundos = 900;

    internal static void AdicionarAutenticacaoJwt(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<ConfiguracaoJwt>()
            .BindConfiguration("Jwt")
            .Validate(config => !string.IsNullOrWhiteSpace(config.ChaveAssinatura) &&
                Encoding.UTF8.GetByteCount(config.ChaveAssinatura) >= 32,
                "Configure Jwt:ChaveAssinatura com no mínimo 32 bytes.")
            .Validate(config => !builder.Environment.IsDevelopment() ||
                (!string.IsNullOrWhiteSpace(config.CodigoAcesso) && config.CodigoAcesso.Length >= 32 &&
                    config.CodigoAcesso != config.ChaveAssinatura),
                "Em Development, configure Jwt:CodigoAcesso com no mínimo 32 caracteres e diferente da chave.")
            .ValidateOnStart();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        builder.Services.AddAuthorization();
        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<ConfiguracaoJwt>>((options, configuracao) =>
            {
                options.MapInboundClaims = false;
                options.SaveToken = false;
                options.IncludeErrorDetails = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = configuracao.Value.ObterChave(),
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateIssuer = true,
                    ValidIssuer = Emissor,
                    ValidateAudience = true,
                    ValidAudience = Audiencia,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
    }

    internal static Results<Ok<TokenJwt>, UnauthorizedHttpResult> EmitirToken(
        SolicitacaoToken request,
        IOptions<ConfiguracaoJwt> configuracao,
        HttpContext context)
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";

        var config = configuracao.Value;
        if (!CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Codigo ?? string.Empty)),
            SHA256.HashData(Encoding.UTF8.GetBytes(config.CodigoAcesso))))
        {
            return TypedResults.Unauthorized();
        }

        var agora = DateTime.UtcNow;
        var jwt = new JwtSecurityToken(
            issuer: Emissor,
            audience: Audiencia,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "demonstracao-local")],
            notBefore: agora,
            expires: agora.AddSeconds(ValidadeEmSegundos),
            signingCredentials: new SigningCredentials(config.ObterChave(), SecurityAlgorithms.HmacSha256));

        return TypedResults.Ok(new TokenJwt(
            new JwtSecurityTokenHandler().WriteToken(jwt), "Bearer", ValidadeEmSegundos));
    }
}

internal sealed class ConfiguracaoJwt
{
    // O binding de configuração preenche estas propriedades durante a inicialização.
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string ChaveAssinatura { get; init; } = string.Empty;
    [UsedImplicitly(ImplicitUseKindFlags.Assign)]
    public string CodigoAcesso { get; init; } = string.Empty;

    internal SymmetricSecurityKey ObterChave() =>
        new(Encoding.UTF8.GetBytes(ChaveAssinatura));
}

// O JSON da requisição é desserializado pelo ASP.NET Core.
[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
internal sealed record SolicitacaoToken(string? Codigo);

// Os membros são lidos pelo serializador JSON da resposta.
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.Members)]
internal sealed record TokenJwt(string AccessToken, string TokenType, int ExpiresIn);
