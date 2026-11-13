# Backend: Vertical Slices com Minimal API e handlers explícitos, sem mediador

Cada caso de uso é um slice em `Features/<Feature>/<CasoDeUso>/`, com um endpoint de Minimal API que chama um handler concreto injetado por construtor. Não há MediatR nem outra biblioteca de mediação. O MediatR passou a ter licença comercial em 2025 e acrescenta uma indireção (request → pipeline → handler resolvido em runtime) que obriga o leitor, humano ou agente, a sair do arquivo para descobrir o que roda. Com poucos casos de uso, `endpoint → handler` explícito se lê num arquivo só. Preocupações transversais (validação, erros, rate limiting) ficam no pipeline do ASP.NET Core, não em behaviors de mediador.

## Consequences

- Layout: um projeto `Joya.Pedidos.Api` com os slices em `Features/` e o compartilhado mínimo em `Infraestrutura/`, mais `Joya.Pedidos.Tests.Unit` e `Joya.Pedidos.Tests.Integration`. Não há projetos Domain/Application/Infrastructure.
- As regras de negócio de cada caso de uso ficam numa função pura dentro do slice, que recebe os dados já carregados e devolve erros ou o resultado, sem exceção para fluxo esperado. O handler só orquestra o I/O.
