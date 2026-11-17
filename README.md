# FIAP 5NETT — Fase 3

API ASP.NET Core em .NET 10 para calcular a média de duas provas e a situação acadêmica.

## Executar localmente

### Com Docker Compose

```sh
docker compose up --build
```

O Compose inicia a API em Development e publica a porta 5080 no endereço local `127.0.0.1`. Para remover o container e a rede da aplicação, execute `docker compose down`.

### Com o SDK .NET

As duas opções usam a porta 5080. Encerre a primeira execução antes de iniciar a outra.

```sh
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/Escola.Api -- --urls http://127.0.0.1:5080
```

Em Development, abra o [Scalar](http://127.0.0.1:5080/scalar/v1) para consultar e testar a operação. O [documento OpenAPI](http://127.0.0.1:5080/openapi/v1.json) também está disponível.

```sh
curl -i http://127.0.0.1:5080/resultados-academicos/calcular \
  -H 'Content-Type: application/json' \
  -d '{"nota1":6,"nota2":8}'
```

O exemplo retorna HTTP `200` com média `7` e situação `Aprovado`. Com uma nota ausente ou fora de 0 a 10, a API retorna HTTP `400` com Problem Details.

## Executar a suíte

```sh
dotnet test Escola.slnx --configuration Release
```
