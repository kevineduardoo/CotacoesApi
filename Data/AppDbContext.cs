using Microsoft.EntityFrameworkCore;
using CotacoesApi.Models;

namespace CotacoesApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cotacao> Cotacoes => Set<Cotacao>();
}