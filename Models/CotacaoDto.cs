namespace CotacoesApi.Models;

public class CotacaoDto
{
    public string Moeda { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateTime DataHora { get; set; }
}