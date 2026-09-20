using CotacoesApi.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;

namespace CotacoesApi.Tests;

public class ApiKeyFilterTests
{
    private static ApiKeyFilter CriarFiltro(string? chaveConfigurada)
    {
        var valores = new Dictionary<string, string?> { ["ApiKey"] = chaveConfigurada };
        var config = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        return new ApiKeyFilter(config);
    }

    private static ActionExecutingContext CriarContexto(string? chaveNoHeader)
    {
        var httpContext = new DefaultHttpContext();
        if (chaveNoHeader is not null)
            httpContext.Request.Headers[ApiKeyFilter.HeaderName] = chaveNoHeader;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }

    // Roda o filtro e informa se ele deixou a ação do controller executar
    private static async Task<bool> Executar(ApiKeyFilter filtro, ActionExecutingContext contexto)
    {
        var executou = false;
        await filtro.OnActionExecutionAsync(contexto, () =>
        {
            executou = true;
            return Task.FromResult(new ActionExecutedContext(contexto, new List<IFilterMetadata>(), new object()));
        });
        return executou;
    }

    [Fact]
    public async Task ChaveCorreta_ExecutaAAcaoSemBloquear()
    {
        var filtro = CriarFiltro("segredo");
        var contexto = CriarContexto("segredo");

        var executou = await Executar(filtro, contexto);

        Assert.True(executou);
        Assert.Null(contexto.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("chave-errada")]
    public async Task ChaveAusenteOuErrada_Retorna401ENaoExecutaAAcao(string? chaveRecebida)
    {
        var filtro = CriarFiltro("segredo");
        var contexto = CriarContexto(chaveRecebida);

        var executou = await Executar(filtro, contexto);

        Assert.False(executou);
        Assert.IsType<UnauthorizedObjectResult>(contexto.Result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ServidorSemChaveConfigurada_Retorna500ENaoExecutaAAcao(string? chaveConfigurada)
    {
        var filtro = CriarFiltro(chaveConfigurada);
        var contexto = CriarContexto("qualquer-coisa");

        var executou = await Executar(filtro, contexto);

        Assert.False(executou);
        var resultado = Assert.IsType<ObjectResult>(contexto.Result);
        Assert.Equal(500, resultado.StatusCode);
    }
}