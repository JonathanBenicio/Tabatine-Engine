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

CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Clientes_OmieId" ON "Clientes" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE INDEX "IX_ItensPedido_PedidoVendaId" ON "ItensPedido" ("PedidoVendaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE INDEX "IX_ItensPedido_ProdutoId" ON "ItensPedido" ("ProdutoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE INDEX "IX_NotasFiscais_ClienteId" ON "NotasFiscais" ("ClienteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_NotasFiscais_OmieId" ON "NotasFiscais" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE INDEX "IX_NotasFiscais_PedidoVendaId" ON "NotasFiscais" ("PedidoVendaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE INDEX "IX_PedidosVenda_ClienteId" ON "PedidosVenda" ("ClienteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_PedidosVenda_OmieId" ON "PedidosVenda" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_Produtos_OmieId" ON "Produtos" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315203515_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315203515_InitialCreate', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315214210_AddSyncState') THEN
    CREATE TABLE "IntegrationSyncStates" (
        "Id" uuid NOT NULL,
        "ModuleName" text NOT NULL,
        "LastSyncDate" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_IntegrationSyncStates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315214210_AddSyncState') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315214210_AddSyncState', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Produtos" ADD "FamiliaProduto" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Produtos" ADD "PesoBruto" numeric(18,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Produtos" ADD "PesoLiquido" numeric(18,4) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Produtos" ADD "UnidadeMedida" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ALTER COLUMN "DataPrevisao" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "CodigoVendedor" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "Faturado" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "ObservacoesVenda" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "QuantidadeVolumes" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "Transportadora" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "UsuarioInclusao" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorFrete" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "HoraEmissao" interval;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "ValorCofinsRetido" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "ValorCsll" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "ValorIr" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "ValorIss" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "NotasFiscais" ADD "ValorPisRetido" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "PercentualDesconto" numeric(5,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "ValorCofins" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "ValorDesconto" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "ValorIcms" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "ValorIpi" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "ItensPedido" ADD "ValorPis" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ALTER COLUMN "InscricaoEstadual" TYPE character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ALTER COLUMN "Estado" TYPE character varying(2);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ALTER COLUMN "Cep" TYPE character varying(10);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "Bairro" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "Endereco" character varying(255);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "EnderecoComplemento" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "EnderecoNumero" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "InscricaoMunicipal" character varying(50);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    ALTER TABLE "Clientes" ADD "OptanteSimplesNacional" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317163152_ExpandEntitiesWithFullData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317163152_ExpandEntitiesWithFullData', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317174158_AddVendedorAndContaCorrente') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317174158_AddVendedorAndContaCorrente') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317174158_AddVendedorAndContaCorrente') THEN
    CREATE UNIQUE INDEX "IX_ContasCorrente_OmieId" ON "ContasCorrente" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317174158_AddVendedorAndContaCorrente') THEN
    CREATE UNIQUE INDEX "IX_Vendedores_OmieId" ON "Vendedores" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317174158_AddVendedorAndContaCorrente') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317174158_AddVendedorAndContaCorrente', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE TABLE "ItensNotaFiscal" (
        "Id" uuid NOT NULL,
        "NotaFiscalId" uuid NOT NULL,
        "ProdutoId" uuid NOT NULL,
        "Quantidade" numeric(18,4) NOT NULL,
        "ValorUnitario" numeric(18,2) NOT NULL,
        "ValorTotal" numeric(18,2) NOT NULL,
        "Cfop" text,
        "Ncm" text,
        CONSTRAINT "PK_ItensNotaFiscal" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ItensNotaFiscal_NotasFiscais_NotaFiscalId" FOREIGN KEY ("NotaFiscalId") REFERENCES "NotasFiscais" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ItensNotaFiscal_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE TABLE "NotaFiscalTitulos" (
        "Id" uuid NOT NULL,
        "NotaFiscalId" uuid NOT NULL,
        "NumeroParcela" integer NOT NULL,
        "Valor" numeric(18,2) NOT NULL,
        "DataVencimento" timestamp with time zone NOT NULL,
        "OmieIdTitulo" bigint,
        CONSTRAINT "PK_NotaFiscalTitulos" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_NotaFiscalTitulos_NotasFiscais_NotaFiscalId" FOREIGN KEY ("NotaFiscalId") REFERENCES "NotasFiscais" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE TABLE "PedidoParcelas" (
        "Id" uuid NOT NULL,
        "PedidoVendaId" uuid NOT NULL,
        "NumeroParcela" integer NOT NULL,
        "Valor" numeric(18,2) NOT NULL,
        "DataVencimento" timestamp with time zone NOT NULL,
        "Percentual" numeric(5,2) NOT NULL,
        CONSTRAINT "PK_PedidoParcelas" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_PedidoParcelas_PedidosVenda_PedidoVendaId" FOREIGN KEY ("PedidoVendaId") REFERENCES "PedidosVenda" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE INDEX "IX_ItensNotaFiscal_NotaFiscalId" ON "ItensNotaFiscal" ("NotaFiscalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE INDEX "IX_ItensNotaFiscal_ProdutoId" ON "ItensNotaFiscal" ("ProdutoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE INDEX "IX_NotaFiscalTitulos_NotaFiscalId" ON "NotaFiscalTitulos" ("NotaFiscalId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    CREATE INDEX "IX_PedidoParcelas_PedidoVendaId" ON "PedidoParcelas" ("PedidoVendaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317184929_ExpandSyncCapabilities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317184929_ExpandSyncCapabilities', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "PedidosVenda" ADD "CodigoContaCorrente" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "PedidosVenda" ADD "ContaCorrenteId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "PedidosVenda" ADD "VendedorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "NotaFiscalTitulos" ADD "CodigoContaCorrente" bigint;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "NotaFiscalTitulos" ADD "ContaCorrenteId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    CREATE INDEX "IX_PedidosVenda_ContaCorrenteId" ON "PedidosVenda" ("ContaCorrenteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    CREATE INDEX "IX_PedidosVenda_VendedorId" ON "PedidosVenda" ("VendedorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    CREATE INDEX "IX_NotaFiscalTitulos_ContaCorrenteId" ON "NotaFiscalTitulos" ("ContaCorrenteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "NotaFiscalTitulos" ADD CONSTRAINT "FK_NotaFiscalTitulos_ContasCorrente_ContaCorrenteId" FOREIGN KEY ("ContaCorrenteId") REFERENCES "ContasCorrente" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "PedidosVenda" ADD CONSTRAINT "FK_PedidosVenda_ContasCorrente_ContaCorrenteId" FOREIGN KEY ("ContaCorrenteId") REFERENCES "ContasCorrente" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    ALTER TABLE "PedidosVenda" ADD CONSTRAINT "FK_PedidosVenda_Vendedores_VendedorId" FOREIGN KEY ("VendedorId") REFERENCES "Vendedores" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317194027_AddSellerAndBankAccountRelations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317194027_AddSellerAndBankAccountRelations', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    ALTER TABLE "PedidosVenda" DROP COLUMN "CodigoContaCorrente";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    ALTER TABLE "PedidosVenda" DROP COLUMN "CodigoVendedor";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    ALTER TABLE "NotaFiscalTitulos" DROP COLUMN "CodigoContaCorrente";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    ALTER TABLE "PedidoParcelas" ADD "ContaCorrenteId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    CREATE INDEX "IX_PedidoParcelas_ContaCorrenteId" ON "PedidoParcelas" ("ContaCorrenteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    ALTER TABLE "PedidoParcelas" ADD CONSTRAINT "FK_PedidoParcelas_ContasCorrente_ContaCorrenteId" FOREIGN KEY ("ContaCorrenteId") REFERENCES "ContasCorrente" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317195717_AddContaCorrenteToParcelasAndCleanupOmieIds', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotasFiscais" ADD "ContaCorrenteId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotasFiscais" ADD "VendedorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotaFiscalTitulos" ADD "VendedorId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    CREATE INDEX "IX_NotasFiscais_ContaCorrenteId" ON "NotasFiscais" ("ContaCorrenteId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    CREATE INDEX "IX_NotasFiscais_VendedorId" ON "NotasFiscais" ("VendedorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    CREATE INDEX "IX_NotaFiscalTitulos_VendedorId" ON "NotaFiscalTitulos" ("VendedorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotaFiscalTitulos" ADD CONSTRAINT "FK_NotaFiscalTitulos_Vendedores_VendedorId" FOREIGN KEY ("VendedorId") REFERENCES "Vendedores" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotasFiscais" ADD CONSTRAINT "FK_NotasFiscais_ContasCorrente_ContaCorrenteId" FOREIGN KEY ("ContaCorrenteId") REFERENCES "ContasCorrente" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    ALTER TABLE "NotasFiscais" ADD CONSTRAINT "FK_NotasFiscais_Vendedores_VendedorId" FOREIGN KEY ("VendedorId") REFERENCES "Vendedores" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317202343_AddVendedorToNfAndTitulos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317202343_AddVendedorToNfAndTitulos', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317210545_PendingChanges') THEN
    ALTER TABLE "NotasFiscais" ADD "CodigoStatus" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317210545_PendingChanges') THEN
    ALTER TABLE "NotasFiscais" ADD "Denegada" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317210545_PendingChanges') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317210545_PendingChanges', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "Autorizado" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "Cancelado" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "CodigoParcela" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "Contato" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "DataInclusao" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "Denegado" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "Devolvido" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    ALTER TABLE "PedidosVenda" ADD "UsuarioAlteracao" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317213330_AddPedidoVendaCamposFaltantes') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317213330_AddPedidoVendaCamposFaltantes', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE TABLE "Bancos" (
        "Id" uuid NOT NULL,
        "CodigoBanco" character varying(10) NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "CodigoIspb" character varying(20),
        "Tipo" character varying(50),
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_Bancos" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE TABLE "EtapasFaturamento" (
        "Id" uuid NOT NULL,
        "Codigo" character varying(20) NOT NULL,
        "Descricao" character varying(100) NOT NULL,
        "DescricaoPadrao" character varying(100),
        "Inativa" boolean NOT NULL,
        "CodigoOperacao" character varying(20) NOT NULL,
        "DescricaoOperacao" character varying(100) NOT NULL,
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_EtapasFaturamento" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE TABLE "FormasPagamento" (
        "Id" uuid NOT NULL,
        "Codigo" character varying(20) NOT NULL,
        "Descricao" character varying(150) NOT NULL,
        "QuantidadeParcelas" integer NOT NULL,
        "DiasParcelas" integer,
        "ListaParcelas" character varying(1000),
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_FormasPagamento" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE UNIQUE INDEX "IX_Bancos_CodigoBanco" ON "Bancos" ("CodigoBanco");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE UNIQUE INDEX "IX_EtapasFaturamento_CodigoOperacao_Codigo" ON "EtapasFaturamento" ("CodigoOperacao", "Codigo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    CREATE UNIQUE INDEX "IX_FormasPagamento_Codigo" ON "FormasPagamento" ("Codigo");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317231916_AddPrioritizedOmieEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317231916_AddPrioritizedOmieEntities', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "PedidosVenda" ADD "EtapaFaturamentoId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "PedidosVenda" ADD "FormaPagamentoId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "ContasCorrente" ADD "BancoId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    CREATE INDEX "IX_PedidosVenda_EtapaFaturamentoId" ON "PedidosVenda" ("EtapaFaturamentoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    CREATE INDEX "IX_PedidosVenda_FormaPagamentoId" ON "PedidosVenda" ("FormaPagamentoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    CREATE INDEX "IX_ContasCorrente_BancoId" ON "ContasCorrente" ("BancoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "ContasCorrente" ADD CONSTRAINT "FK_ContasCorrente_Bancos_BancoId" FOREIGN KEY ("BancoId") REFERENCES "Bancos" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "PedidosVenda" ADD CONSTRAINT "FK_PedidosVenda_EtapasFaturamento_EtapaFaturamentoId" FOREIGN KEY ("EtapaFaturamentoId") REFERENCES "EtapasFaturamento" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    ALTER TABLE "PedidosVenda" ADD CONSTRAINT "FK_PedidosVenda_FormasPagamento_FormaPagamentoId" FOREIGN KEY ("FormaPagamentoId") REFERENCES "FormasPagamento" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318002608_AddEtapaFormaRelacionamentos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318002608_AddEtapaFormaRelacionamentos', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "BaseCalculoIcms" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "PesoBruto" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "PesoLiquido" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "PrevisaoEntrega" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorCofins" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorIcms" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorIpi" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorMercadorias" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "PedidosVenda" ADD "ValorPis" numeric(18,2) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "ItensPedido" ADD "PesoBruto" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    ALTER TABLE "ItensPedido" ADD "PesoLiquido" numeric(18,3) NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318011753_ExpandPedidoData') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318011753_ExpandPedidoData', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318013941_AddMeiosPagamento') THEN
    CREATE TABLE "MeiosPagamento" (
        "Id" uuid NOT NULL,
        "Codigo" character varying(20) NOT NULL,
        "Descricao" character varying(150) NOT NULL,
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_MeiosPagamento" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318013941_AddMeiosPagamento') THEN
    CREATE UNIQUE INDEX "IX_MeiosPagamento_OmieId" ON "MeiosPagamento" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318013941_AddMeiosPagamento') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318013941_AddMeiosPagamento', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014250_AddCaracteristicas') THEN
    CREATE TABLE "Caracteristicas" (
        "Id" uuid NOT NULL,
        "Nome" character varying(100) NOT NULL,
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_Caracteristicas" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014250_AddCaracteristicas') THEN
    CREATE TABLE "CaracteristicaValores" (
        "Id" uuid NOT NULL,
        "CaracteristicaId" uuid NOT NULL,
        "Valor" character varying(255) NOT NULL,
        "OmieIdConteudo" bigint,
        CONSTRAINT "PK_CaracteristicaValores" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_CaracteristicaValores_Caracteristicas_CaracteristicaId" FOREIGN KEY ("CaracteristicaId") REFERENCES "Caracteristicas" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014250_AddCaracteristicas') THEN
    CREATE UNIQUE INDEX "IX_Caracteristicas_OmieId" ON "Caracteristicas" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014250_AddCaracteristicas') THEN
    CREATE INDEX "IX_CaracteristicaValores_CaracteristicaId" ON "CaracteristicaValores" ("CaracteristicaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014250_AddCaracteristicas') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318014250_AddCaracteristicas', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    CREATE TABLE "TabelasPreco" (
        "Id" uuid NOT NULL,
        "Nome" character varying(150) NOT NULL,
        "Codigo" character varying(20) NOT NULL,
        "Ativa" boolean NOT NULL,
        "OmieId" bigint NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        "OmieUpdatedAt" timestamp with time zone,
        CONSTRAINT "PK_TabelasPreco" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    CREATE TABLE "TabelaPrecoItens" (
        "Id" uuid NOT NULL,
        "TabelaPrecoId" uuid NOT NULL,
        "ProdutoId" uuid NOT NULL,
        "Valor" numeric(18,2) NOT NULL,
        CONSTRAINT "PK_TabelaPrecoItens" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TabelaPrecoItens_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_TabelaPrecoItens_TabelasPreco_TabelaPrecoId" FOREIGN KEY ("TabelaPrecoId") REFERENCES "TabelasPreco" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    CREATE INDEX "IX_TabelaPrecoItens_ProdutoId" ON "TabelaPrecoItens" ("ProdutoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    CREATE INDEX "IX_TabelaPrecoItens_TabelaPrecoId" ON "TabelaPrecoItens" ("TabelaPrecoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    CREATE UNIQUE INDEX "IX_TabelasPreco_OmieId" ON "TabelasPreco" ("OmieId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318014440_AddTabelaPrecos') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318014440_AddTabelaPrecos', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    ALTER TABLE "PedidoParcelas" ADD "MeioPagamentoId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    ALTER TABLE "ItensPedido" ADD "TabelaPrecoId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE TABLE "ProdutoCaracteristicas" (
        "Id" uuid NOT NULL,
        "ProdutoId" uuid NOT NULL,
        "CaracteristicaId" uuid NOT NULL,
        "CaracteristicaValorId" uuid NOT NULL,
        CONSTRAINT "PK_ProdutoCaracteristicas" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ProdutoCaracteristicas_CaracteristicaValores_Caracteristica~" FOREIGN KEY ("CaracteristicaValorId") REFERENCES "CaracteristicaValores" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ProdutoCaracteristicas_Caracteristicas_CaracteristicaId" FOREIGN KEY ("CaracteristicaId") REFERENCES "Caracteristicas" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_ProdutoCaracteristicas_Produtos_ProdutoId" FOREIGN KEY ("ProdutoId") REFERENCES "Produtos" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE INDEX "IX_PedidoParcelas_MeioPagamentoId" ON "PedidoParcelas" ("MeioPagamentoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE INDEX "IX_ItensPedido_TabelaPrecoId" ON "ItensPedido" ("TabelaPrecoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE INDEX "IX_ProdutoCaracteristicas_CaracteristicaId" ON "ProdutoCaracteristicas" ("CaracteristicaId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE INDEX "IX_ProdutoCaracteristicas_CaracteristicaValorId" ON "ProdutoCaracteristicas" ("CaracteristicaValorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    CREATE INDEX "IX_ProdutoCaracteristicas_ProdutoId" ON "ProdutoCaracteristicas" ("ProdutoId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    ALTER TABLE "ItensPedido" ADD CONSTRAINT "FK_ItensPedido_TabelasPreco_TabelaPrecoId" FOREIGN KEY ("TabelaPrecoId") REFERENCES "TabelasPreco" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    ALTER TABLE "PedidoParcelas" ADD CONSTRAINT "FK_PedidoParcelas_MeiosPagamento_MeioPagamentoId" FOREIGN KEY ("MeioPagamentoId") REFERENCES "MeiosPagamento" ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318015129_RefineRelationships') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318015129_RefineRelationships', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318041034_AddPendingFields') THEN

                    DO $$ BEGIN
                        ALTER TABLE "PedidosVenda" ADD COLUMN "ComissaoVendedor" numeric(10,2) NOT NULL DEFAULT 0.0;
                    EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                    DO $$ BEGIN
                        ALTER TABLE "NotasFiscais" ADD COLUMN "ImportadoApi" boolean NOT NULL DEFAULT false;
                    EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                    DO $$ BEGIN
                        ALTER TABLE "NotasFiscais" ADD COLUMN "NaturezaOperacao" character varying(200);
                    EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                    DO $$ BEGIN
                        ALTER TABLE "NotasFiscais" ADD COLUMN "Serie" character varying(20);
                    EXCEPTION WHEN duplicate_column THEN NULL; END $$;

                    DO $$ BEGIN
                        ALTER TABLE "ItensPedido" ADD COLUMN "UnidadeMedida" character varying(10);
                    EXCEPTION WHEN duplicate_column THEN NULL; END $$;
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318041034_AddPendingFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318041034_AddPendingFields', '10.0.5');
    END IF;
END $EF$;
COMMIT;

