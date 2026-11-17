# FIAP 5NETT — Fase 3

API ASP.NET Core em .NET 10 para calcular a média de duas provas e a situação acadêmica.

## Executar localmente

Crie o arquivo `.env` na raiz com dois valores aleatórios independentes. Execute este comando uma vez em um clone novo:

```sh
(umask 077; set -C; printf 'ESCOLA_JWT_CHAVE=%s\nESCOLA_JWT_CODIGO=%s\n' "$(openssl rand -hex 32)" "$(openssl rand -hex 32)" > .env)
```

### Com Docker Compose

```sh
docker compose up --build
```

O Compose inicia a API em Development e publica a porta 5080 no endereço local `127.0.0.1`. Os links da emissão de token, da API, do Scalar e do OpenAPI aparecem nos logs junto com o horário de início em São Paulo.

Para iniciar em outra porta, use `ESCOLA_PORTA_LOCAL=5081 docker compose up --build`. Para remover o container e a rede da aplicação, execute `docker compose down`.

### Com o SDK .NET

As duas opções usam a porta 5080. Encerre a primeira execução antes de iniciar a outra.

```sh
. ./.env
ASPNETCORE_ENVIRONMENT=Development \
  Jwt__ChaveAssinatura="$ESCOLA_JWT_CHAVE" \
  Jwt__CodigoAcesso="$ESCOLA_JWT_CODIGO" \
  dotnet run --project src/Escola.Api -- --urls http://127.0.0.1:5080
```

Em Development, abra o [Scalar](http://127.0.0.1:5080/scalar/v1) para consultar e testar a operação. O [documento OpenAPI](http://127.0.0.1:5080/openapi/v1.json) também está disponível.

### Autenticar e calcular

1. No Scalar, execute `POST /auth/token` com um JSON cujo campo `codigo` contém o valor de `ESCOLA_JWT_CODIGO` do arquivo `.env`.
2. Copie o `accessToken` da resposta para a autenticação Bearer do Scalar. O token dura 15 minutos. A emissão local só está disponível em Development.
3. Execute `POST /resultados-academicos/calcular` com as duas notas. Para usar o terminal, defina a variável `TOKEN` com o token emitido e rode:

```sh
curl -i http://127.0.0.1:5080/resultados-academicos/calcular \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d '{"nota1":6,"nota2":8}'
```

O exemplo retorna HTTP `200` com média `7` e situação `Aprovado`. Com uma nota ausente ou fora de 0 a 10, a API retorna HTTP `400` com Problem Details. Um token ausente, inválido ou expirado recebe HTTP `401`.

## Testar no Postman

1. Inicie a API com `docker compose up --build`.
2. No Postman Desktop, clique em **Import** e selecione a [coleção](postman/fiap-5nett-fase-3.postman_collection.json) e o [ambiente](postman/fiap-5nett-local.postman_environment.json).
3. Selecione **FIAP 5NETT — Local**. Preencha o valor **local** de `codigoAcesso` com `ESCOLA_JWT_CODIGO` do `.env`. A `baseUrl` já aponta para a porta 5080. Se a API estiver em outra porta, ajuste também o valor local de `baseUrl`.
4. Envie **Obter token JWT**. O script guarda `accessToken` no ambiente, e as requisições protegidas usam esse valor automaticamente. Emita um novo token após 15 minutos.
5. Envie uma requisição de cálculo e confira **Test Results**. Para executar todos os casos, abra a coleção e escolha **Run collection** com o mesmo ambiente. Mantenha **Obter token JWT** como primeira requisição.

Para rodar a mesma coleção no terminal interno do Postman Desktop ou com o [Postman CLI independente](https://learning.postman.com/docs/postman-cli/postman-cli-installation), use Python 3.9+ na raiz:

```sh
python3 postman/run.py
python3 postman/run.py --porta 5081
```

O runner lê o código do `.env`, monta um ambiente temporário para o endereço local e o remove ao terminar. A chave de assinatura fica com a API.

## Executar a suíte

```sh
dotnet test Escola.slnx --configuration Release
```
