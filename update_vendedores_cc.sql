CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Clientes" (
    "Id" uuid NOT NULL,
    "RazaoSocial" character varying(255) NOT NULL,
    "NomeFantasia" text NOT NULL,
    "CnpjCpf" character varying(20) NOT NULL,
    "Email" text,
    "Telefone" text,
    "InscricaoEstadual" text,
    "Cep" text,
    "Estado" text,
    "Cidade" text,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Clientes" PRIMARY KEY ("Id")
);

CREATE TABLE "Produtos" (
    "Id" uuid NOT NULL,
    "CodigoProduto" character varying(50) NOT NULL,
    "Descricao" character varying(255) NOT NULL,
    "Ncm" text NOT NULL,
    "Ean" text,
    "PrecoUnitario" numeric(18,2) NOT NULL,
    "Ativo" boolean NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Produtos" PRIMARY KEY ("Id")
);

CREATE TABLE "PedidosVenda" (
    "Id" uuid NOT NULL,
    "NumeroPedido" text NOT NULL,
    "Etapa" text NOT NULL,
    "ValorTotal" numeric(18,2) NOT NULL,
    "DataPrevisao" timestamp with time zone NOT NULL,
    "ClienteId" uuid NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_PedidosVenda" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PedidosVenda_Clientes_ClienteId" FOREIGN KEY ("ClienteId") REFERENCES "Clientes" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ItensPedido" (
    "Id" uuid NOT NULL,
    "PedidoVendaId" uuid NOT NULL,
    "ProdutoId" uuid NOT NULL,
    "Quantidade" integer NOT NULL,
    "ValorUnitario" numeric(18,2) NOT NULL,
    "ValorTotal" numeric(18,2) NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_ItensPedido" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ItensPedido_PedidosVenda_PedidoVendaId" FOREIGN KEY ("PedidoVendaId") REFERENCES "PedidosVenda" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ItensPedido_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "NotasFiscais" (
    "Id" uuid NOT NULL,
    "NumeroNf" character varying(50) NOT NULL,
    "ChaveAcesso" character varying(100) NOT NULL,
    "Status" text NOT NULL,
    "DataEmissao" timestamp with time zone NOT NULL,
    "ValorTotal" numeric(18,2) NOT NULL,
    "PedidoVendaId" uuid,
    "ClienteId" uuid NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_NotasFiscais" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_NotasFiscais_Clientes_ClienteId" FOREIGN KEY ("ClienteId") REFERENCES "Clientes" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_NotasFiscais_PedidosVenda_PedidoVendaId" FOREIGN KEY ("PedidoVendaId") REFERENCES "PedidosVenda" ("Id") ON DELETE SET NULL
);

CREATE UNIQUE INDEX "IX_Clientes_OmieId" ON "Clientes" ("OmieId");

CREATE INDEX "IX_ItensPedido_PedidoVendaId" ON "ItensPedido" ("PedidoVendaId");

CREATE INDEX "IX_ItensPedido_ProdutoId" ON "ItensPedido" ("ProdutoId");

CREATE INDEX "IX_NotasFiscais_ClienteId" ON "NotasFiscais" ("ClienteId");

CREATE UNIQUE INDEX "IX_NotasFiscais_OmieId" ON "NotasFiscais" ("OmieId");

CREATE INDEX "IX_NotasFiscais_PedidoVendaId" ON "NotasFiscais" ("PedidoVendaId");

CREATE INDEX "IX_PedidosVenda_ClienteId" ON "PedidosVenda" ("ClienteId");

CREATE UNIQUE INDEX "IX_PedidosVenda_OmieId" ON "PedidosVenda" ("OmieId");

CREATE UNIQUE INDEX "IX_Produtos_OmieId" ON "Produtos" ("OmieId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260315203515_InitialCreate', '10.0.5');

COMMIT;

START TRANSACTION;
CREATE TABLE "IntegrationSyncStates" (
    "Id" uuid NOT NULL,
    "ModuleName" text NOT NULL,
    "LastSyncDate" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_IntegrationSyncStates" PRIMARY KEY ("Id")
);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260315214210_AddSyncState', '10.0.5');

COMMIT;

START TRANSACTION;
ALTER TABLE "Produtos" ADD "FamiliaProduto" text;

ALTER TABLE "Produtos" ADD "PesoBruto" numeric(18,4) NOT NULL DEFAULT 0.0;

ALTER TABLE "Produtos" ADD "PesoLiquido" numeric(18,4) NOT NULL DEFAULT 0.0;

ALTER TABLE "Produtos" ADD "UnidadeMedida" text;

ALTER TABLE "PedidosVenda" ALTER COLUMN "DataPrevisao" DROP NOT NULL;

ALTER TABLE "PedidosVenda" ADD "CodigoVendedor" bigint;

ALTER TABLE "PedidosVenda" ADD "Faturado" boolean NOT NULL DEFAULT FALSE;

ALTER TABLE "PedidosVenda" ADD "ObservacoesVenda" text;

ALTER TABLE "PedidosVenda" ADD "QuantidadeVolumes" integer NOT NULL DEFAULT 0;

ALTER TABLE "PedidosVenda" ADD "Transportadora" text;

ALTER TABLE "PedidosVenda" ADD "UsuarioInclusao" text;

ALTER TABLE "PedidosVenda" ADD "ValorFrete" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "NotasFiscais" ADD "HoraEmissao" interval;

ALTER TABLE "NotasFiscais" ADD "ValorCofinsRetido" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "NotasFiscais" ADD "ValorCsll" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "NotasFiscais" ADD "ValorIr" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "NotasFiscais" ADD "ValorIss" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "NotasFiscais" ADD "ValorPisRetido" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "PercentualDesconto" numeric(5,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "ValorCofins" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "ValorDesconto" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "ValorIcms" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "ValorIpi" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "ItensPedido" ADD "ValorPis" numeric(18,2) NOT NULL DEFAULT 0.0;

ALTER TABLE "Clientes" ALTER COLUMN "InscricaoEstadual" TYPE character varying(50);

ALTER TABLE "Clientes" ALTER COLUMN "Estado" TYPE character varying(2);

ALTER TABLE "Clientes" ALTER COLUMN "Cep" TYPE character varying(10);

ALTER TABLE "Clientes" ADD "Bairro" character varying(100);

ALTER TABLE "Clientes" ADD "Endereco" character varying(255);

ALTER TABLE "Clientes" ADD "EnderecoComplemento" text;

ALTER TABLE "Clientes" ADD "EnderecoNumero" text;

ALTER TABLE "Clientes" ADD "InscricaoMunicipal" character varying(50);

ALTER TABLE "Clientes" ADD "OptanteSimplesNacional" boolean NOT NULL DEFAULT FALSE;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260317163152_ExpandEntitiesWithFullData', '10.0.5');

COMMIT;

START TRANSACTION;
CREATE TABLE "ContasCorrente" (
    "Id" uuid NOT NULL,
    "Descricao" character varying(100) NOT NULL,
    "CodigoIntegracao" character varying(50),
    "Tipo" character varying(20) NOT NULL,
    "Inativa" boolean NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_ContasCorrente" PRIMARY KEY ("Id")
);

CREATE TABLE "Vendedores" (
    "Id" uuid NOT NULL,
    "Nome" character varying(150) NOT NULL,
    "Email" character varying(100),
    "Comissao" numeric(18,2) NOT NULL,
    "Inativo" boolean NOT NULL,
    "OmieId" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    "OmieUpdatedAt" timestamp with time zone,
    CONSTRAINT "PK_Vendedores" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX "IX_ContasCorrente_OmieId" ON "ContasCorrente" ("OmieId");

CREATE UNIQUE INDEX "IX_Vendedores_OmieId" ON "Vendedores" ("OmieId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260317174158_AddVendedorAndContaCorrente', '10.0.5');

COMMIT;

