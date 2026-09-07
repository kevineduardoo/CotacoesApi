# CotacoesApi

API REST em ASP.NET Core que consome uma API externa de câmbio, armazena histórico de cotações e expõe endpoints com cache, persistência e tratamento de erros.

🔗 **API no ar:** https://cotacoesapi-production-2cf6.up.railway.app/swagger

## Tecnologias

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + SQLite
- IMemoryCache
- Swagger / Swashbuckle
- Docker (deploy)
- API externa: [Frankfurter](https://frankfurter.dev/)

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/cotacoes/atual?moeda=USD` | Retorna a cotação atual (cache de 5 min) |
| GET | `/cotacoes/historico?moeda=USD` | Retorna as últimas 50 cotações salvas |
| POST | `/cotacoes/atualizar?moeda=USD` | Força atualização, ignorando o cache |

## Como rodar localmente

```bash
git clone https://github.com/kevineduardoo/CotacoesApi.git
cd CotacoesApi/CotacoesApi
dotnet restore
dotnet run
```

Acesse `http://localhost:5297/swagger` para testar os endpoints.

## Decisões técnicas

- **Cache em memória (5 min):** evita bater na API externa a cada requisição, reduzindo latência e risco de rate limiting.
- **Persistência de histórico:** toda cotação buscada é salva no SQLite via EF Core, permitindo consultar dados sem depender da API externa.
- **Middleware de erro global:** qualquer exceção não tratada retorna um JSON padronizado, sem vazar stack trace.
- **Troca de API externa:** o projeto inicialmente usava a AwesomeAPI, mas ela apresentou rate limiting (429) persistente em produção. Migrado para a Frankfurter, mais estável para esse tipo de uso.

## Próximos passos

- Migrar o SQLite para um banco persistente (ex: PostgreSQL), já que o Railway não mantém o arquivo SQLite entre reinicializações do container.
- Adicionar testes automatizados.
- Adicionar autenticação por API key nos endpoints de escrita.

## Deploy

Deploy feito no [Railway](https://railway.app) via Dockerfile, com build automático a cada push na branch `main`.