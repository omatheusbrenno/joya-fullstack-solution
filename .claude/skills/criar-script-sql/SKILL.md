---
name: criar-script-sql
description: Padrão dos scripts SQL de database/ (idempotentes, aplicados em ordem pelo db-init, com índices justificados). Use ao criar ou mudar uma tabela, um índice, um login, uma carga ou uma query de investigação em database/.
---

# Criar um script SQL

Os scripts de `database/` são a fonte única do schema (ADR 0003): não há migrations. O `db-init` do compose (`database/init/aplicar-scripts.sh`) aplica, em ordem, todo `database/[0-9][0-9]-*.sql` com o usuário `sa`, **em todo `docker compose up`**. Por isso todo script é _idempotente_: rodar duas vezes dá o mesmo banco que rodar uma vez.

Os mesmos arquivos rodam também no **Azure SQL Database** do deploy (ADR 0016), conectados direto em `JoyaPedidos` com o admin do servidor. Lá o `USE` só aceita o banco atual e não há login no `master`: veja [Logins e segredos](#logins-e-segredos).

Leia o script vivo mais parecido antes de escrever:

- **Banco**: `00-criar-banco.sql`.
- **Tabelas, restrições e índices**: `01-criar-schema.sql`.
- **Login com privilégio mínimo e senha por variável**: `02-criar-login-api.sql` (leitura e escrita) e `05-criar-login-mcp.sql` (só leitura + `SHOWPLAN`).
- **Carga legível com ids fixos**: `03-carga-exemplo.sql`.
- **Carga com volume determinístico**: `04-carga-volume.sql`.
- **Query de investigação, fora do db-init**: `database/parte-4/`.

## Passos

1. **Nome e lugar.** Use `NN-verbo-objeto.sql`, com o próximo número livre, para o que o `db-init` aplica. Uma query que só se lê ou se mede vai numa subpasta (`database/parte-4/`), fora do padrão `NN-*.sql`.
2. **Cabeçalho.** Abra com comentários que dizem:
   - o que o script faz;
   - _como_ ele é idempotente;
   - de qual script depende (ex.: "Depende de 03-carga-exemplo.sql").
3. **Script**, com a guarda de idempotência de cada objeto (tabela abaixo) e `GO` entre os lotes.
4. **Testes de integração.** O `BancoDeTestes` aplica os mesmos arquivos num SQL Server do Testcontainers (ADR 0006):
   - um script de schema ou de login entra em `ScriptsDeCriacao`;
   - uma variável `$(X)` nova ganha o `.Replace` em `AplicarScriptAsync`;
   - uma carga é aplicada pelo próprio teste com `banco.AplicarScriptAsync(...)`, **duas vezes**, e o assert confere que nada duplicou (ver `CargaDeExemploTests`);
   - um login se prova pela conexão dele: o que ele lê e o erro 229 no que ele não pode, com o assert sobre a própria conexão (ver `LoginDoMcpTests`).
5. **Verificação.** Rode `docker compose run --rm db-init` duas vezes seguidas contra o banco do compose: as duas terminam em "Banco pronto.". Depois rode os testes de integração.

## Idempotência: a guarda de cada objeto

| Objeto | Guarda |
| --- | --- |
| Banco | `IF DB_ID(N'JoyaPedidos') IS NULL CREATE DATABASE ...` |
| Tabela | `IF OBJECT_ID(N'dbo.Tabela', N'U') IS NULL BEGIN CREATE TABLE ... END` |
| Índice | `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_...' AND object_id = OBJECT_ID(N'dbo.Tabela'))` |
| Login | `IF SUSER_ID(N'login') IS NULL EXEC (N'CREATE LOGIN ...') ELSE EXEC (N'ALTER LOGIN ... WITH PASSWORD ...')`, só fora do Azure: a senha se realinha com o `.env` |
| Usuário | SQL Server: `IF USER_ID(N'login') IS NULL EXEC (N'CREATE USER ... FOR LOGIN ...')`. Azure: usuário contido, `CREATE USER ... WITH PASSWORD` ou `ALTER USER ... WITH PASSWORD` |
| Papel e permissão | `ALTER ROLE ... ADD MEMBER` e `GRANT` já são idempotentes |
| Linha de carga | `INSERT ... SELECT ... FROM (VALUES ...) AS v WHERE NOT EXISTS (SELECT 1 FROM dbo.X WHERE Id = v.Id)` |

A guarda olha só se o objeto existe. Mudar a definição de uma tabela que já existe em `01` não altera um banco já criado. Para mudar uma coluna ou restrição, acrescente um bloco guardado pelo estado que você quer (ex.: `IF COL_LENGTH(N'dbo.Tabela', N'Coluna') IS NULL ALTER TABLE ...`). Recriar o banco com `docker compose down -v` apaga os dados: peça ao humano.

## Schema

- Termos de domínio do `GLOSSARY.md` em PT-BR, tabela no plural (`dbo.Pedidos`, `dbo.ItensPedido`). Sempre `dbo.` e literais `N'...'`.
- Toda restrição tem nome: `PK_Tabela`, `FK_Tabela_Referenciada`, `CK_Tabela_Coluna`, `UQ_Tabela_ColunaA_ColunaB`.
- Tipos: dinheiro em `DECIMAL(18, 2)`, nunca ponto flutuante; data em `DATETIME2(0)`, sempre UTC; texto em `NVARCHAR(n)`.
- A regra de negócio que o banco consegue garantir vira `CHECK` ou `UNIQUE` (Estoque `>= 0`, Quantidade `BETWEEN 1 AND 10000`, Produto único no Pedido). Ela também fica na regra pura da API.

## Índices

- Cada índice nasce de uma query real e leva um comentário de uma linha dizendo qual (ex.: `-- Filtro por Cliente + Período e Top Clientes.`). Nome: `IX_Tabela_Coluna1_Coluna2`.
- Chave = colunas de filtro e de junção, na ordem de seletividade da query. `INCLUDE` = colunas só lidas, para a query não fazer lookup (ex.: `IX_ItensPedido_PedidoId ... INCLUDE (Quantidade, PrecoUnitario)` calcula o Valor Total, ADR 0009).
- Toda FK ganha um índice na coluna que referencia.
- A query que usa o índice filtra a coluna sem função: Período é o intervalo semiaberto `col >= @Inicio AND col < @FimExclusivo`, com o parâmetro no mesmo tipo da coluna. `YEAR(col)`, `CAST(col ...)` ou conversão implícita impedem o seek.
- Confirme pelo MCP (`.mcp.json`, login `joya_mcp`): `EXPLAIN ANALYZE <query>` para o plano real e `SET STATISTICS IO, TIME ON` para leituras e tempo. Um scan é a escolha certa quando o filtro cobre boa parte da tabela (ver README, Parte 4). Nesse caso, o índice não ajuda e não deve entrar.

## Logins e segredos

- Um login por consumidor, com o mínimo de papel (OWASP A01): a API tem `db_datareader` + `db_datawriter`; o MCP tem `db_datareader` + `SHOWPLAN`. Nenhum tem DDL.
- **Dois motores** (ADR 0016), separados por `CAST(SERVERPROPERTY('EngineEdition') AS INT) = 5` (Azure SQL Database). No SQL Server, login + usuário; no Azure, usuário contido. Sem `USE master`: `CREATE LOGIN` vale em qualquer banco do SQL Server. O DDL de cada motor fica em `EXEC (N'...')`, com as aspas da senha dobradas, porque o Azure recusa `CREATE LOGIN` até num ramo que não executa. Copie `02-criar-login-api.sql`. Um login só do ambiente local (como o do MCP, em `05`) não cria nada no Azure.
- `CREATE LOGIN ... WITH PASSWORD = N'$(SENHA_X)', CHECK_POLICY = ON`. A senha nunca entra no script (A02), nem contém `'`. Ela percorre este caminho:
  1. `x-senhas` do `docker-compose.yml`, com padrão de desenvolvimento (ADR 0012);
  2. `-v SENHA_X="$SENHA_X"` em `aplicar-scripts.sh`;
  3. uma linha em `.env.example`;
  4. o `.Replace("$(SENHA_X)", ...)` do `BancoDeTestes`.
- A política do SQL Server recusa senha que contém o nome do login.

## Carga

- Abra com `SET NOCOUNT ON; SET XACT_ABORT ON;` e ponha tudo num `BEGIN TRANSACTION ... COMMIT`: um erro desfaz a carga inteira.
- Ids fixos com `SET IDENTITY_INSERT dbo.X ON/OFF` e guarda `NOT EXISTS` por id. Um volume grande usa uma faixa de ids reservada, documentada no cabeçalho, para não colidir com o que a API criou.
- Volume _determinístico_: cada valor deriva do número da linha por aritmética modular, a partir de uma tabela de números (`ROW_NUMBER()` sobre `sys.all_objects` cruzado). Documente as fórmulas no cabeçalho: os testes e o E2E conferem literais calculados a partir delas.

## Commits

Um commit por script verde, em Conventional Commits PT-BR, com o escopo `database` ou `carga` (ex.: `feat(database): cria o login somente leitura do MCP`).
