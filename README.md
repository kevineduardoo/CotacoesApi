# CotacoesApi
![CI](https://github.com/kevineduardoo/CotacoesApi/actions/workflows/ci.yml/badge.svg)

API REST em ASP.NET Core que consome uma API externa de câmbio, armazena histórico de cotações e expõe endpoints com cache, persistência e tratamento de erros.

🔗 **API no ar:** https://cotacoesapi-production-2cf6.up.railway.app/swagger

## Tecnologias

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core + PostgreSQL (produção) e SQLite (desenvolvimento local)
- IMemoryCache
- Swagger / Swashbuckle
- Docker (deploy)
- API externa: [Frankfurter](https://frankfurter.dev/)

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/cotacoes/atual?moeda=USD` | Retorna a cotação atual (cache de 5 min) |
| GET | `/cotacoes/historico?moeda=USD` | Retorna as últimas 50 cotações salvas |
| POST | `/cotacoes/atualizar?moeda=USD` | Força atualização, ignorando o cache (requer API key) |

## Autenticação

O endpoint `POST /cotacoes/atualizar` exige uma API key no cabeçalho `X-Api-Key`. Sem a chave, ou com uma chave errada, a API responde `401`. Os endpoints `GET` são públicos.

```bash
curl -X POST "https://cotacoesapi-production-2cf6.up.railway.app/cotacoes/atualizar?moeda=USD" \
  -H "X-Api-Key: SUA_CHAVE"
```

Para rodar localmente, configure a chave com user-secrets:

```bash
dotnet user-secrets set "ApiKey" "sua-chave-local"
```

Em produção, a chave fica na variável de ambiente `ApiKey` do Railway.

No Swagger, clique em **Authorize** e informe a chave para testar o endpoint protegido.

## Como rodar localmente

```bash
git clone https://github.com/kevineduardoo/CotacoesApi.git
cd CotacoesApi
dotnet restore
dotnet run
```

Acesse `http://localhost:5297/swagger` para testar os endpoints.

## Testes

O projeto tem 22 testes automatizados com xUnit, que rodam a cada push e pull request via GitHub Actions.

```bash
dotnet test CotacoesApi.Tests
```

- **Serviço da API externa:** usa um `HttpMessageHandler` falso para simular sucesso, erros de status (429, 500, 404) e JSON inválido, sem depender da internet.
- **Controller:** usa SQLite em memória para testar cache, histórico, filtro por moeda, limite de 50 registros e erro 502.

## Decisões técnicas

- **Cache em memória (5 min):** evita bater na API externa a cada requisição, reduzindo latência e risco de rate limiting.
- **Persistência de histórico:** toda cotação buscada é salva no banco via EF Core, permitindo consultar dados sem depender da API externa. Em produção usa PostgreSQL no Railway, com volume persistente; localmente usa SQLite.
- **Middleware de erro global:** qualquer exceção não tratada retorna um JSON padronizado, sem vazar stack trace.
- **Troca de API externa:** o projeto inicialmente usava a AwesomeAPI, mas ela apresentou rate limiting (429) persistente em produção. Migrado para a Frankfurter, mais estável para esse tipo de uso.
- **API key em filtro de ação:** o endpoint de escrita é protegido por um filtro que compara a chave em tempo constante e bloqueia tudo se o servidor estiver sem chave configurada.
- **Migrations do Entity Framework Core:** o esquema do banco é versionado por migrations, aplicadas com Migrate() na inicialização. O ambiente local também usa PostgreSQL, para o comportamento ficar igual ao de produção.

## Próximos passos


## Deploy

Deploy feito no [Railway](https://railway.app) via Dockerfile, com build automático a cada push na branch `main`.