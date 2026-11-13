# Testes de integração com Testcontainers, aplicando os scripts de `database/`

Os testes de integração sobem o próprio SQL Server via Testcontainers e aplicam os mesmos scripts de criação de `database/`, em vez de depender do banco do `docker compose up`. Assim, os testes rodam igual na máquina e no GitHub Actions, começam com o banco num estado conhecido e ainda provam que os scripts entregues funcionam.
