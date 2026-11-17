using System.Collections.Concurrent;
using System.Net;
using Allure.Net.Commons.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Escola.IntegrationTests;

[AllureParentSuite("Testes HTTP")]
[AllureSuite("Links de inicialização")]
public sealed class InicializacaoHttpTests(EscolaApiFactory factory) : IClassFixture<EscolaApiFactory>
{
    [Theory]
    [InlineData("")]
    [InlineData("http://127.0.0.1:5080")]
    public async Task Inicializacao_publica_documentacao_e_registra_links_quando_configurados(string endereco)
    {
        using var logs = new CapturaLogs();
        using var application = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Escola:EnderecoLocal"] = endereco,
                }));
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
        });
        using var client = application.CreateClient();
        using var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var startup = logs.Mensagens.Where(message => message.Contains("Iniciado em")).ToArray();
        if (endereco.Length == 0)
        {
            Assert.Empty(startup);
            return;
        }

        var message = Assert.Single(startup);
        Assert.Contains($"Token (POST): {endereco}/auth/token", message);
        Assert.Contains($"API (POST): {endereco}/resultados-academicos/calcular", message);
        Assert.Contains($"Scalar: {endereco}/scalar/v1", message);
        Assert.Contains($"OpenAPI: {endereco}/openapi/v1.json", message);
        Assert.Matches(@"Iniciado em \d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2} [+-]\d{2}:\d{2}", message);
    }

    private sealed class CapturaLogs : ILoggerProvider, ILogger
    {
        internal ConcurrentQueue<string> Mensagens { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public bool IsEnabled(LogLevel logLevel) => true;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Dispose() { }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Mensagens.Enqueue(formatter(state, exception));
    }
}
