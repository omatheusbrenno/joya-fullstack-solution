# Backend: Dapper, com os scripts SQL de `database/` como fonte única do schema

O schema nasce dos scripts de criação e carga em `database/`, que o enunciado exige de qualquer forma. O acesso a dados usa Dapper com SQL parametrizado escrito no próprio slice. Não há EF Core nem migrations.

## Considered Options

- **EF Core com migrations**: criaria duas fontes de verdade para o schema (migrations e scripts) e esconderia o SQL que a Parte 4 (debug de query) e os índices exigem que fique visível.
- **EF Core sem migrations, só como mapeador**: paga o custo de change tracking e de LINQ traduzido sem ganhar o que o EF tem de melhor.

## Consequences

- O mapeamento de linhas para objetos é escrito à mão. Com quatro tabelas, é pouco.
- A Baixa de Estoque usa um `UPDATE` condicional (`WHERE Estoque >= @quantidade`) na mesma transação da criação do Pedido. Zero linhas afetadas significa Estoque insuficiente, o que fecha a corrida entre conferir e baixar.
- Todo SQL é parametrizado. Concatenar entrada do usuário em SQL é proibido (OWASP A05, injeção).
