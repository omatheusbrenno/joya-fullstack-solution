-- Schema da Gestão de Pedidos (fonte única do schema, ADR 0003).
-- Idempotente: cada objeto só é criado se ainda não existir.
USE JoyaPedidos;
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes (
        Id   INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Clientes PRIMARY KEY,
        Nome NVARCHAR(200)      NOT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos (
        Id      INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Produtos PRIMARY KEY,
        Nome    NVARCHAR(200)      NOT NULL,
        Preco   DECIMAL(18, 2)     NOT NULL CONSTRAINT CK_Produtos_Preco CHECK (Preco >= 0),
        Estoque INT                NOT NULL CONSTRAINT CK_Produtos_Estoque CHECK (Estoque >= 0)
    );
END
GO

IF OBJECT_ID(N'dbo.Pedidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pedidos (
        Id         INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Pedidos PRIMARY KEY,
        ClienteId  INT                NOT NULL CONSTRAINT FK_Pedidos_Clientes REFERENCES dbo.Clientes (Id),
        DataPedido DATETIME2(0)       NOT NULL -- sempre UTC, definida pelo servidor
    );
END
GO

IF OBJECT_ID(N'dbo.ItensPedido', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItensPedido (
        Id            INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ItensPedido PRIMARY KEY,
        PedidoId      INT                NOT NULL CONSTRAINT FK_ItensPedido_Pedidos REFERENCES dbo.Pedidos (Id),
        ProdutoId     INT                NOT NULL CONSTRAINT FK_ItensPedido_Produtos REFERENCES dbo.Produtos (Id),
        Quantidade    INT                NOT NULL CONSTRAINT CK_ItensPedido_Quantidade CHECK (Quantidade BETWEEN 1 AND 10000),
        PrecoUnitario DECIMAL(18, 2)     NOT NULL CONSTRAINT CK_ItensPedido_PrecoUnitario CHECK (PrecoUnitario >= 0),
        CONSTRAINT UQ_ItensPedido_PedidoId_ProdutoId UNIQUE (PedidoId, ProdutoId)
    );
END
GO

-- Filtro por Período e ordenação da listagem.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_DataPedido ON dbo.Pedidos (DataPedido) INCLUDE (ClienteId);
GO

-- Filtro por Cliente + Período e Top Clientes.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_ClienteId_DataPedido' AND object_id = OBJECT_ID(N'dbo.Pedidos'))
    CREATE INDEX IX_Pedidos_ClienteId_DataPedido ON dbo.Pedidos (ClienteId, DataPedido);
GO

-- Valor Total do Pedido sem lookup (ADR 0009).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_PedidoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
    CREATE INDEX IX_ItensPedido_PedidoId ON dbo.ItensPedido (PedidoId) INCLUDE (Quantidade, PrecoUnitario);
GO

-- Suporte à FK de Produto.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ItensPedido_ProdutoId' AND object_id = OBJECT_ID(N'dbo.ItensPedido'))
    CREATE INDEX IX_ItensPedido_ProdutoId ON dbo.ItensPedido (ProdutoId);
GO
