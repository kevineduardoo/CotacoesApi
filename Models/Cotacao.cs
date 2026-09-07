namespace CotacoesApi.Models;

public class Cotacao
{
    public int Id { get; set; }
    public string Moeda { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataHora { get; set; }
}