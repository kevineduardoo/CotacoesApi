using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CotacoesApi.Filters;

public class ApiKeyFilter : IAsyncActionFilter
{
    public const string HeaderName = "X-Api-Key";

    private readonly IConfiguration _configuration;

    public ApiKeyFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var chaveEsperada = _configuration["ApiKey"];

        // Sem chave configurada no servidor: bloqueia tudo, em vez de liberar por engano
        if (string.IsNullOrWhiteSpace(chaveEsperada))
        {
            context.Result = new ObjectResult(new { erro = "Autenticação não configurada no servidor." })
            {
                StatusCode = 500
            };
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var chaveRecebida)
            || !ChavesIguais(chaveEsperada, chaveRecebida.ToString()))
        {
            context.Result = new UnauthorizedObjectResult(new { erro = "API key ausente ou inválida." });
            return;
        }

        await next();
    }

    private static bool ChavesIguais(string esperada, string recebida) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(esperada),
            Encoding.UTF8.GetBytes(recebida));
}