using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CotacoesApi.Data;
using CotacoesApi.Models;
using CotacoesApi.Services;

namespace CotacoesApi.Controllers;

[ApiController]
[Route("cotacoes")]
public class CotacoesController : ControllerBase
{
    private readonly ICotacaoExternaService _externaService;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public CotacoesController(ICotacaoExternaService externaService, AppDbContext db, IMemoryCache cache)
    {
        _externaService = externaService;
        _db = db;
        _cache = cache;
    }

    [HttpGet("atual")]
    public async Task<IActionResult> ObterAtual([FromQuery] string moeda = "USD")
    {
        var cacheKey = $"cotacao_{moeda.ToUpper()}";

        if (_cache.TryGetValue(cacheKey, out CotacaoDto? cotacaoCache))
            return Ok(cotacaoCache);

        var cotacao = await _externaService.ObterCotacaoAtualAsync(moeda);
        if (cotacao is null)
            return StatusCode(502, new { erro = "Não foi possível obter a cotação externa." });

        _db.Cotacoes.Add(new Models.Cotacao
        {
            Moeda = cotacao.Moeda,
            Valor = cotacao.Valor,
            DataHora = cotacao.DataHora
        });
        await _db.SaveChangesAsync();

        _cache.Set(cacheKey, cotacao, TimeSpan.FromMinutes(5));

        return Ok(cotacao);
    }

    [HttpGet("historico")]
    public async Task<IActionResult> ObterHistorico([FromQuery] string? moeda)
    {
        var query = _db.Cotacoes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(moeda))
            query = query.Where(c => c.Moeda == moeda.ToUpper());

        var resultado = await query
            .OrderByDescending(c => c.DataHora)
            .Take(50)
            .ToListAsync();

        return Ok(resultado);
    }

    [HttpPost("atualizar")]
    public async Task<IActionResult> AtualizarManualmente([FromQuery] string moeda = "USD")
    {
        var cacheKey = $"cotacao_{moeda.ToUpper()}";
        _cache.Remove(cacheKey);

        var cotacao = await _externaService.ObterCotacaoAtualAsync(moeda);
        if (cotacao is null)
            return StatusCode(502, new { erro = "Não foi possível atualizar a cotação." });

        _db.Cotacoes.Add(new Models.Cotacao
        {
            Moeda = cotacao.Moeda,
            Valor = cotacao.Valor,
            DataHora = cotacao.DataHora
        });
        await _db.SaveChangesAsync();

        return Ok(new { mensagem = "Cotação atualizada com sucesso.", cotacao });
    }
}