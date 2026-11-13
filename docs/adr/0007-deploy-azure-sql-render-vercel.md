# Deploy: Azure SQL Database (Free Offer), backend no Render e frontend na Vercel

O deploy online é diferencial, não obrigatório, e foi planejado como a última fatia, depois de todo o obrigatório pronto. A CI no GitHub Actions (build e testes a cada push e PR) entra logo no começo. Escolhemos serviços com tier gratuito: Azure SQL Database Free Offer (SQL Server gerenciado), o container do backend no Render e o frontend estático na Vercel. Custo conhecido: o tier gratuito do Render hiberna e leva cerca de 50 s para atender a primeira requisição, o que o README deve avisar.
