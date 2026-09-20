using CotacoesApi.Controllers;
using CotacoesApi.Data;
using CotacoesApi.Models;
using CotacoesApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using CotacaoEntidade = CotacoesApi.Models.Cotacao;

namespace CotacoesApi.Tests;

public class CotacoesControllerTests : IDisposable
{
    // Serviço falso: devolve uma resposta pronta e conta quantas vezes foi chamado
    private class FakeExternaService : ICotacaoExternaService
    {
        public int Chamadas { get; private set; }

        public CotacaoDto? Resposta { get; set; } = new CotacaoDto
        {
            Moeda = "USD",
            Valor = 5.25m,
            DataHora = DateTime.UtcNow
        };

        public Task<CotacaoDto?> ObterCotacaoAtualAsync(string moeda)
        {
            Chamadas++;
            return Task.FromResult(Resposta);
        }
    }

    private readonly SqliteConnection _conexao;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly FakeExternaService _externa;
    private readonly CotacoesController _controller;

    // O xUnit cria uma instância nova da classe para cada teste,
    // então cada teste começa com banco e cache limpos
    public CotacoesControllerTests()
    {
        _conexao = new SqliteConnection("DataSource=:memory:");
        _conexao.Open(); // o banco em memória some se a conexão fechar

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_conexao)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _cache = new MemoryCache(new MemoryCacheOptions());
        _externa = new FakeExternaService();
        _controller = new CotacoesController(_externa, _db, _cache);
    }

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
        _conexao.Dispose();
    }

    private static CotacaoEntidade NovaCotacao(string moeda, decimal valor, DateTime dataHora) =>
        new() { Moeda = moeda, Valor = valor, DataHora = dataHora };

    // ---------- GET /cotacoes/atual ----------

    [Fact]
    public async Task ObterAtual_SemCache_BuscaNaApiSalvaNoBancoERetornaOk()
    {
        // Act
        var resultado = await _controller.ObterAtual("USD");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(resultado);
        var dto = Assert.IsType<CotacaoDto>(ok.Value);
        Assert.Equal(5.25m, dto.Valor);
        Assert.Equal(1, _externa.Chamadas);
        Assert.Equal(1, await _db.Cotacoes.CountAsync());
    }

    [Fact]
    public async Task ObterAtual_SegundaChamada_UsaCacheENaoChamaApiExterna()
    {
        await _controller.ObterAtual("USD");
        await _controller.ObterAtual("USD");

        Assert.Equal(1, _externa.Chamadas);
        Assert.Equal(1, await _db.Cotacoes.CountAsync());
    }

    [Fact]
    public async Task ObterAtual_MoedaEmMinusculo_UsaMesmaChaveDeCache()
    {
        await _controller.ObterAtual("usd");
        await _controller.ObterAtual("USD");

        Assert.Equal(1, _externa.Chamadas);
    }

    [Fact]
    public async Task ObterAtual_ApiExternaFalha_Retorna502ENaoSalvaNada()
    {
        _externa.Resposta = null;

        var resultado = await _controller.ObterAtual("USD");

        var erro = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(502, erro.StatusCode);
        Assert.Equal(0, await _db.Cotacoes.CountAsync());
    }

    // ---------- POST /cotacoes/atualizar ----------

    [Fact]
    public async Task AtualizarManualmente_IgnoraCacheESalvaNovaCotacao()
    {
        await _controller.ObterAtual("USD");

        var resultado = await _controller.AtualizarManualmente("USD");

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(2, _externa.Chamadas);
        Assert.Equal(2, await _db.Cotacoes.CountAsync());
    }

    [Fact]
    public async Task AtualizarManualmente_ApiExternaFalha_Retorna502()
    {
        _externa.Resposta = null;

        var resultado = await _controller.AtualizarManualmente("USD");

        var erro = Assert.IsType<ObjectResult>(resultado);
        Assert.Equal(502, erro.StatusCode);
    }

    // ---------- GET /cotacoes/historico ----------

    [Fact]
    public async Task ObterHistorico_ComFiltroDeMoeda_RetornaSoDaMoeda()
    {
        _db.Cotacoes.AddRange(
            NovaCotacao("USD", 5.0m, DateTime.UtcNow),
            NovaCotacao("EUR", 6.0m, DateTime.UtcNow),
            NovaCotacao("USD", 5.1m, DateTime.UtcNow.AddMinutes(1)));
        await _db.SaveChangesAsync();

        var resultado = await _controller.ObterHistorico("usd");

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<IEnumerable<CotacaoEntidade>>(ok.Value).ToList();
        Assert.Equal(2, lista.Count);
        Assert.All(lista, c => Assert.Equal("USD", c.Moeda));
    }

    [Fact]
    public async Task ObterHistorico_MuitosRegistros_RetornaOs50MaisRecentes()
    {
        var agora = DateTime.UtcNow;
        for (var i = 0; i < 60; i++)
            _db.Cotacoes.Add(NovaCotacao("USD", i, agora.AddMinutes(i)));
        await _db.SaveChangesAsync();

        var resultado = await _controller.ObterHistorico(null);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var lista = Assert.IsAssignableFrom<IEnumerable<CotacaoEntidade>>(ok.Value).ToList();
        Assert.Equal(50, lista.Count);
        Assert.Equal(59m, lista.First().Valor); // o mais recente vem primeiro
    }
}