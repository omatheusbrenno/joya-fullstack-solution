# Joya — Mini Gestão de Pedidos

Teste técnico Full Stack (.NET + TypeScript, AI First): cadastro e consulta de Pedidos de Clientes, de ponta a ponta, desenvolvido com Claude Code.

> Em construção. Este README vai documentar como rodar com Docker, como rodar os testes, as decisões técnicas, o uso do Claude e o debug da query.

## Configuração AI First (já disponível)

- `CLAUDE.md` na raiz, mais `backend/CLAUDE.md` e `frontend/CLAUDE.md`, com o contexto de cada app.
- `.claude/skills/`: skills de engenharia de [mattpocock/skills](https://github.com/mattpocock/skills), instaladas com `npx skills add`. Versões fixadas em `skills-lock.json`; para atualizar, `npx skills update`.
- `.claude/rules/`: regras de segurança (OWASP Top 10:2025), carregadas por caminho.
- `.claude/hooks/`: hook que bloqueia reescrita do histórico git.
- `docs/agents/`: issue tracker (GitHub Issues + Projects), rótulos de triagem e layout dos docs de domínio.
- `.mcp.json`: MCP do SQL Server (DBHub) com um login somente leitura. Ver [MCP do SQL Server](#mcp-do-sql-server).

## MCP do SQL Server

O Claude Code lê o banco do `docker compose` pelo [DBHub](https://github.com/bytebase/dbhub) (`@bytebase/dbhub@1.5.0`, versão fixa), declarado no `.mcp.json` versionado. Ao abrir o projeto, o Claude Code pede para aprovar o servidor `sqlserver`. Com o compose no ar, ele tem duas ferramentas:

- `search_objects`: lista tabelas, colunas e índices.
- `execute_sql`: roda SQL. `EXPLAIN <query>` devolve o plano estimado (o DBHub liga `SET SHOWPLAN_XML`); `EXPLAIN ANALYZE <query>` executa e devolve o plano real (`SET STATISTICS XML`). As mensagens de `SET STATISTICS IO, TIME ON` voltam junto do resultado.

**Somente leitura pela permissão, não pela ferramenta (ADR 0010).** O DBHub conecta com o login `joya_mcp`, criado por `database/05-criar-login-mcp.sql` em todo `docker compose up`, que tem só `db_datareader` e `SHOWPLAN`. Um `INSERT` ou `DELETE` falha no SQL Server com "The INSERT permission was denied", qualquer que seja o cliente. Os testes de `LoginDoMcpTests` provam isso contra um SQL Server real. O modo `readonly` do DBHub fica desligado: ele bloquearia o `SET STATISTICS` da investigação, e seria só mais uma camada sobre a garantia que já está no login.

A senha vem da variável `SENHA_MCP`. Sem ela, valem o padrão de desenvolvimento do compose e do `.mcp.json` (ADR 0012), que só alcança o SQL Server publicado em `127.0.0.1`.

### Como o MCP foi usado

Na Parte 4, todo número abaixo saiu do MCP, com o mesmo comando do `.mcp.json` e o login `joya_mcp`. O `.mcp.json` nasceu no meio da sessão, e o Claude Code só carrega servidores MCP ao iniciar. Por isso o Claude falou com o DBHub por um cliente MCP mínimo via stdio: as mesmas mensagens `tools/call`, sem reiniciar a sessão.

1. `search_objects` para conferir as tabelas da carga.
2. `execute_sql` com `SET STATISTICS IO, TIME ON` na query original e na corrigida, três vezes cada. A tabela abaixo traz a terceira, com cache quente.
3. `EXPLAIN ANALYZE` das duas, para ler os operadores, as linhas estimadas contra as reais e as leituras por operador.
4. A mesma medição num Período seletivo (outubro de 2026), para isolar o efeito do `YEAR()` sobre o seek.
5. A saída da corrigida comparada com `GET /relatorios/top-clientes?dataInicio=2026-01-01&dataFim=2026-12-31`: as mesmas cinco linhas, com os mesmos valores.

Um `INSERT` e um `DELETE` tentados pelo MCP voltaram negados pelo SQL Server, o que confirma a garantia do login na prática.

## Parte 4: debug da query de Top Clientes

A query do enunciado deveria devolver os Clientes que mais compraram em 2026. A original e a corrigida estão lado a lado em `database/parte-4/`, fora do padrão `NN-*.sql`, então o `db-init` não as executa:

- [`top-clientes-original.sql`](database/parte-4/top-clientes-original.sql), sem alteração;
- [`top-clientes-corrigida.sql`](database/parte-4/top-clientes-corrigida.sql), a mesma lógica do endpoint `GET /relatorios/top-clientes`.

```sql
SELECT c.Nome, SUM(i.Quantidade * i.PrecoUnitario) AS Total
FROM Clientes c
LEFT JOIN Pedidos p ON p.ClienteId = c.Id
LEFT JOIN ItensPedido i ON i.PedidoId = p.Id
WHERE YEAR(p.DataPedido) = 2026
GROUP BY c.Nome
ORDER BY Total
```

Ambiente da medição: SQL Server 2022 (CU28) do compose, com 50.019 Pedidos e 100.028 Itens (a carga de `03` e `04` mais 7 Pedidos criados pelo E2E). 22.141 Pedidos (44%) caem em 2026.

### O que estava errado

1. **O `WHERE` anula o `LEFT JOIN`.** Num Cliente sem Pedido, `p.DataPedido` é `NULL`. `YEAR(NULL) = 2026` não é verdadeiro, então a linha cai. O `LEFT JOIN` promete os Clientes sem compra e o `WHERE` os descarta: o resultado é o de um `INNER JOIN`, só que mais caro. O plano mostra isso. Um **Merge Join (Left Outer Join)** gera 50.020 linhas, incluindo os Clientes sem Pedido com `NULL`, e só depois um **Filter** `datepart(year, DataPedido) = 2026` joga fora 27.879 delas. Para um top de quem mais comprou, Cliente sem compra não entra, então a correção é o `INNER JOIN` explícito. Se o pedido fosse listar todos os Clientes com zero, o filtro de data iria para o `ON`.
2. **O `YEAR()` sobre a coluna impede o seek.** Com uma função em volta, o SQL Server não usa a ordem do índice `IX_Pedidos_DataPedido` e precisa avaliar `YEAR()` linha a linha. Ele também perde o histograma: estimou 28.350 linhas para as 22.141 reais. A correção é o intervalo semiaberto `DataPedido >= '2026-01-01' AND DataPedido < '2027-01-01'`, sem função na coluna, que estima 22.138. É o mesmo Período `[início, fim + 1 dia)` da API.
3. **O `GROUP BY c.Nome` soma homônimos.** Os Clientes 1 e 4 se chamam "Ana Souza". A original devolve uma "Ana Souza" com R$ 32.821.025,50, que é a soma de duas pessoas (R$ 17.480.723,70 + R$ 15.340.301,80). A correção agrupa por `ClienteId` e só depois busca o nome (RN13).
4. **A ordem é crescente.** `ORDER BY Total` sem `DESC` põe quem menos comprou no topo: a original começa por "Elisa Rocha". A correção usa `ORDER BY ValorTotal DESC`, com desempate por `ClienteId` para a ordem ser estável.
5. **Falta o limite de cinco.** A original devolve um total por nome para todos com compra em 2026: 7 linhas para 8 Clientes, porque os homônimos se fundem. A correção usa `TOP (5)`.

### Resultado: antes e depois

| Original (7 linhas, crescente) | | Corrigida (5 linhas) | | |
| --- | --- | --- | --- | --- |
| **Nome** | **Total** | **Cliente** | **Pedidos** | **Valor Total** |
| Elisa Rocha | 4.324.470,40 | 1 · Ana Souza | 4.790 | 17.480.723,70 |
| Fernanda Alves | 4.392.813,00 | 4 · Ana Souza | 4.193 | 15.340.301,80 |
| Diego Ferreira | 6.574.396,80 | 7 · Carlos Lima | 3.587 | 13.078.698,80 |
| Carla Mendes | 8.760.262,50 | 2 · Bruno Lima | 2.993 | 10.888.696,40 |
| Bruno Lima | 10.888.696,40 | 3 · Carla Mendes | 2.392 | 8.760.262,50 |
| Carlos Lima | 13.078.698,80 | | | |
| Ana Souza (1 + 4 somados) | 32.821.025,50 | | | |

### Evidência: plano de execução e `STATISTICS IO/TIME`

Período de 2026 inteiro, cache quente:

| | Original | Corrigida |
| --- | --- | --- |
| Leituras lógicas: Pedidos | 134 (Index Scan em `IX_Pedidos_ClienteId_DataPedido`) | 145 (Clustered Index Scan em `PK_Pedidos`) |
| Leituras lógicas: ItensPedido | 507 (Index Scan) | 507 (Index Scan) |
| Leituras lógicas: Clientes | 2 | 10 (5 seeks, só nos vencedores) |
| Tempo de CPU / decorrido | ≈ 51 ms / ≈ 50 ms | ≈ 24 ms / ≈ 23 ms |
| Linhas estimadas × reais do filtro de data | 28.350 × 22.141 | 22.138 × 22.141 |

Operadores do plano real:

- **Original**: Clustered Index Scan em Clientes → Index Scan em Pedidos → **Merge Join (Left Outer)** com 50.020 linhas → **Filter** `YEAR()` → **Hash Match (Right Outer Join)** com os 100.028 Itens, gerando 44.274 linhas → **Hash Match (Aggregate)** por Nome → **Sort** crescente.
- **Corrigida**: Index Scan em `IX_ItensPedido_PedidoId` → **Stream Aggregate** do Valor Total por Pedido, que aproveita a ordem do índice → **Merge Join** com os 22.141 Pedidos de 2026 (as duas entradas já vêm ordenadas pelo id do Pedido) → **Hash Match (Aggregate)** por `ClienteId`, com 8 linhas → **Sort** → **Nested Loops** com **Clustered Index Seek** em Clientes, só para os 5 → **Top**.

Para o ano inteiro, as leituras ficam praticamente iguais e o tempo cai pela metade. O ganho vem da forma do plano: a original carrega 50 mil linhas por um join externo e faz um hash join com os 100 mil Itens; a corrigida agrega os Itens num passe ordenado e junta por merge.

O plano da corrigida **não faz seek em Pedidos para o ano inteiro, e está certo**. 44% da tabela cai em 2026: ler as 145 páginas do índice clusterizado em ordem custa menos do que um seek seguido de um hash join. Por isso não entrou índice novo. Os que a query usa (`IX_Pedidos_DataPedido` e `IX_ItensPedido_PedidoId ... INCLUDE (Quantidade, PrecoUnitario)`) já estão em `01-criar-schema.sql`.

### O seek num Período seletivo

Para isolar o efeito do `YEAR()`, a corrigida rodou em outubro de 2026 (2.031 Pedidos, 4% da tabela) com as duas formas de filtro:

| | `YEAR(DataPedido) = 2026 AND MONTH(DataPedido) = 10` | `DataPedido >= '2026-10-01' AND DataPedido < '2026-11-01'` |
| --- | --- | --- |
| Acesso a Pedidos | Clustered Index Scan em `PK_Pedidos` | **Index Seek** em `IX_Pedidos_DataPedido` |
| Leituras lógicas: Pedidos | 145 | **9** |
| Linhas estimadas × reais | 3.138 × 2.031 | 1.830 × 2.031 |

Com a função, o SQL Server lê a tabela inteira para achar 4% dela. Com o intervalo, vai direto ao trecho do índice: 16 vezes menos páginas. Nos dois casos os Itens continuam num Index Scan de cerca de 500 páginas: agregá-los num passe ordenado custa menos do que 2 mil seeks. O tempo total fica em ≈ 20 ms nos dois, por causa desse scan. A diferença cresce com o volume: o scan custa a tabela inteira, e o seek só o trecho do Período.
