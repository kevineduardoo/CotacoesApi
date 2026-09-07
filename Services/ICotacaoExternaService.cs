using CotacoesApi.Models;

namespace CotacoesApi.Services;

public interface ICotacaoExternaService
{
    Task<CotacaoDto?> ObterCotacaoAtualAsync(string moeda);
}