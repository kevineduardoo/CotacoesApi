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
            var moedaUpper = moeda.ToUpper();
            var url = $"https://api.frankfurter.dev/v1/latest?base={moedaUpper}&symbols=BRL";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("API externa retornou {StatusCode} para {Moeda}", response.StatusCode, moedaUpper);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var rates = doc.RootElement.GetProperty("rates");
            var valor = rates.GetProperty("BRL").GetDecimal();

            return new CotacaoDto
            {
                Moeda = moedaUpper,
                Valor = valor,
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