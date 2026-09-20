using System.Net;
using System.Text;
using CotacoesApi.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CotacoesApi.Tests;

public class CotacaoExternaServiceTests
{
    // Faz o papel da API externa: devolve uma resposta pronta, sem usar a internet
    private class FakeHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _resposta;
        public HttpRequestMessage? UltimaRequisicao { get; private set; }

        public FakeHandler(HttpResponseMessage resposta) => _resposta = resposta;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            UltimaRequisicao = request;
            return Task.FromResult(_resposta);
        }
    }

    private static (CotacaoExternaService service, FakeHandler handler) CriarServico(
        HttpStatusCode status, string corpo)
    {
        var resposta = new HttpResponseMessage(status)
        {
            Content = new StringContent(corpo, Encoding.UTF8, "application/json")
        };
        var handler = new FakeHandler(resposta);
        var service = new CotacaoExternaService(
            new HttpClient(handler),
            NullLogger<CotacaoExternaService>.Instance);
        return (service, handler);
    }

    [Fact]
    public async Task ObterCotacaoAtualAsync_RespostaValida_RetornaCotacao()
    {
        // Arrange
        var json = "{\"amount\":1.0,\"base\":\"USD\",\"date\":\"2026-09-18\",\"rates\":{\"BRL\":5.25}}";
        var (service, _) = CriarServico(HttpStatusCode.OK, json);

        // Act
        var resultado = await service.ObterCotacaoAtualAsync("USD");

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("USD", resultado.Moeda);
        Assert.Equal(5.25m, resultado.Valor);
    }

    [Fact]
    public async Task ObterCotacaoAtualAsync_MoedaEmMinusculo_ConverteParaMaiusculo()
    {
        var json = "{\"rates\":{\"BRL\":5.25}}";
        var (service, handler) = CriarServico(HttpStatusCode.OK, json);

        var resultado = await service.ObterCotacaoAtualAsync("usd");

        Assert.NotNull(resultado);
        Assert.Equal("USD", resultado.Moeda);
        Assert.Contains("base=USD", handler.UltimaRequisicao!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task ObterCotacaoAtualAsync_ApiExternaComErro_RetornaNull(HttpStatusCode status)
    {
        var (service, _) = CriarServico(status, "{}");

        var resultado = await service.ObterCotacaoAtualAsync("USD");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterCotacaoAtualAsync_JsonInvalido_RetornaNull()
    {
        var (service, _) = CriarServico(HttpStatusCode.OK, "isto não é um json");

        var resultado = await service.ObterCotacaoAtualAsync("USD");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterCotacaoAtualAsync_JsonSemBRL_RetornaNull()
    {
        var (service, _) = CriarServico(HttpStatusCode.OK, "{\"rates\":{\"EUR\":0.9}}");

        var resultado = await service.ObterCotacaoAtualAsync("USD");

        Assert.Null(resultado);
    }
}