using System.Text.Json;
using CotacoesApi.Models;

namespace CotacoesApi.Services;

public class CotacaoExternaService : ICotacaoExternaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CotacaoExternaService> _logger;

    public CotacaoExternaService(HttpClient httpClient, ILogger<CotacaoExternaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CotacaoDto?> ObterCotacaoAtualAsync(string moeda)
    {
        try
        {
            var par = $"{moeda.ToUpper()}-BRL";
            var url = $"https://economia.awesomeapi.com.br/json/last/{par}";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API externa retornou {StatusCode} para {Par}", response.StatusCode, par);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var chave = par.Replace("-", "");
            var item = doc.RootElement.GetProperty(chave);

            return new CotacaoDto
            {
                Moeda = moeda.ToUpper(),
                Valor = decimal.Parse(item.GetProperty("bid").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                DataHora = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar cotação para {Moeda}", moeda);
            return null;
        }
    }
}