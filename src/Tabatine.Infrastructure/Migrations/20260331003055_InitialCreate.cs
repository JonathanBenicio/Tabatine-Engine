using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bancos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_banco = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    codigo_ispb = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bancos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    nome_fantasia = table.Column<string>(type: "text", nullable: false),
                    cnpj_cpf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "text", nullable: true),
                    telefone = table.Column<string>(type: "text", nullable: true),
                    inscricao_estadual = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    inscricao_municipal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    optante_simples_nacional = table.Column<bool>(type: "boolean", nullable: false),
                    cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    cidade = table.Column<string>(type: "text", nullable: true),
                    endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    endereco_numero = table.Column<string>(type: "text", nullable: true),
                    endereco_complemento = table.Column<string>(type: "text", nullable: true),
                    bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "condicoes_pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false),
                    dia_fixo = table.Column<int>(type: "integer", nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_condicoes_pagamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "etapas_faturamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descricao_padrao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    inativa = table.Column<bool>(type: "boolean", nullable: false),
                    codigo_operacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao_operacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_etapas_faturamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "formas_pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false),
                    dias_parcelas = table.Column<int>(type: "integer", nullable: true),
                    lista_parcelas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_formas_pagamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "integration_sync_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_name = table.Column<string>(type: "text", nullable: false),
                    last_sync_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_integration_sync_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "text", nullable: true),
                    message_template = table.Column<string>(type: "text", nullable: true),
                    level = table.Column<string>(type: "text", nullable: true),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    exception = table.Column<string>(type: "text", nullable: true),
                    properties = table.Column<string>(type: "text", nullable: true),
                    log_event = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meios_pagamento",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_meios_pagamento", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "perfis",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: true),
                    telegram_link_token = table.Column<Guid>(type: "uuid", nullable: true),
                    telegram_link_token_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_perfis", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "produtos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_produto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ncm = table.Column<string>(type: "text", nullable: false),
                    ean = table.Column<string>(type: "text", nullable: true),
                    unidade_medida = table.Column<string>(type: "text", nullable: true),
                    peso_liquido = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    peso_bruto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    familia_produto = table.Column<string>(type: "text", nullable: true),
                    preco_unitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_produtos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_locks",
                columns: table => new
                {
                    lock_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    lock_token = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_locks", x => x.lock_key);
                });

            migrationBuilder.CreateTable(
                name: "vendedores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    comissao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    inativo = table.Column<bool>(type: "boolean", nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vendedores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    @event = table.Column<string>(name: "event", type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    message_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "contas_corrente",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    codigo_integracao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inativa = table.Column<bool>(type: "boolean", nullable: false),
                    banco_id = table.Column<Guid>(type: "uuid", nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contas_corrente", x => x.id);
                    table.ForeignKey(
                        name: "fk_contas_corrente_bancos_banco_id",
                        column: x => x.banco_id,
                        principalTable: "bancos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pedidos_venda",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_pedido = table.Column<string>(type: "text", nullable: false),
                    etapa = table.Column<string>(type: "text", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_previsao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valor_frete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    transportadora = table.Column<string>(type: "text", nullable: true),
                    quantidade_volumes = table.Column<int>(type: "integer", nullable: false),
                    peso_bruto = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    peso_liquido = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    previsao_entrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valor_icms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_ipi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_pis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_cofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    base_calculo_icms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_mercadorias = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_iss = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_ir = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_csll = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_inss = table.Column<decimal>(type: "numeric", nullable: false),
                    comissao_vendedor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    frete_modalidade = table.Column<string>(type: "text", nullable: true),
                    codigo_parcela = table.Column<string>(type: "text", nullable: true),
                    contato = table.Column<string>(type: "text", nullable: true),
                    observacoes_venda = table.Column<string>(type: "text", nullable: true),
                    observacoes_internas = table.Column<string>(type: "text", nullable: true),
                    dados_adicionais_nf = table.Column<string>(type: "text", nullable: true),
                    meio_pagamento = table.Column<string>(type: "text", nullable: true),
                    quantidade_parcelas = table.Column<int>(type: "integer", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_ibs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_cbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    codigo_rastreio = table.Column<string>(type: "text", nullable: true),
                    link_rastreio = table.Column<string>(type: "text", nullable: true),
                    veiculo_proprio = table.Column<string>(type: "text", nullable: true),
                    placa = table.Column<string>(type: "text", nullable: true),
                    valor_seguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_outras_despesas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    numero_pedido_cliente = table.Column<string>(type: "text", nullable: true),
                    consumidor_final = table.Column<string>(type: "text", nullable: true),
                    data_inclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    usuario_inclusao = table.Column<string>(type: "text", nullable: true),
                    usuario_alteracao = table.Column<string>(type: "text", nullable: true),
                    faturado = table.Column<bool>(type: "boolean", nullable: false),
                    cancelado = table.Column<bool>(type: "boolean", nullable: false),
                    devolvido = table.Column<bool>(type: "boolean", nullable: false),
                    autorizado = table.Column<bool>(type: "boolean", nullable: false),
                    denegado = table.Column<bool>(type: "boolean", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    etapa_faturamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forma_pagamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    condicao_pagamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedidos_venda", x => x.id);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_condicoes_pagamento_condicao_pagamento_id",
                        column: x => x.condicao_pagamento_id,
                        principalTable: "condicoes_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_etapas_faturamento_etapa_faturamento_id",
                        column: x => x.etapa_faturamento_id,
                        principalTable: "etapas_faturamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_formas_pagamento_forma_pagamento_id",
                        column: x => x.forma_pagamento_id,
                        principalTable: "formas_pagamento",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedidos_venda_vendedores_vendedor_id",
                        column: x => x.vendedor_id,
                        principalTable: "vendedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "itens_pedido",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_venda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    unidade_medida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    cfop = table.Column<string>(type: "text", nullable: true),
                    valor_icms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_ipi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_pis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_cofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    percentual_desconto = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    base_icms = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_icms = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_icms = table.Column<string>(type: "text", nullable: true),
                    base_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_ipi = table.Column<string>(type: "text", nullable: true),
                    base_pis = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_pis = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_pis = table.Column<string>(type: "text", nullable: true),
                    base_cofins = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_cofins = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_cofins = table.Column<string>(type: "text", nullable: true),
                    peso_bruto = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    peso_liquido = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    valor_ibs = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_ibs = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_cbs = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_cbs = table.Column<decimal>(type: "numeric", nullable: false),
                    base_ibs_cbs = table.Column<decimal>(type: "numeric", nullable: false),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_pedido", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_pedido_pedidos_venda_pedido_venda_id",
                        column: x => x.pedido_venda_id,
                        principalTable: "pedidos_venda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_pedido_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_nf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    chave_acesso = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    codigo_status = table.Column<int>(type: "integer", nullable: false),
                    data_emissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    hora_emissao = table.Column<TimeSpan>(type: "interval", nullable: true),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    natureza_operacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    modelo = table.Column<string>(type: "text", nullable: true),
                    importado_api = table.Column<bool>(type: "boolean", nullable: false),
                    data_saida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hora_saida = table.Column<TimeSpan>(type: "interval", nullable: true),
                    id_transportadora = table.Column<long>(type: "bigint", nullable: true),
                    valor_iss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_ir = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_csll = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_pis_retido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_cofins_retido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_frete = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_seguro = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_desconto = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_outras_despesas = table.Column<decimal>(type: "numeric", nullable: false),
                    issqn_base_calculo = table.Column<decimal>(type: "numeric", nullable: false),
                    tipo_operacao = table.Column<string>(type: "text", nullable: true),
                    finalidade = table.Column<string>(type: "text", nullable: true),
                    ambiente = table.Column<string>(type: "text", nullable: true),
                    informacoes_complementares = table.Column<string>(type: "text", nullable: true),
                    informacoes_fisco = table.Column<string>(type: "text", nullable: true),
                    valor_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_pis = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_cofins = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_prod = table.Column<decimal>(type: "numeric", nullable: false),
                    icms_base_calculo = table.Column<decimal>(type: "numeric", nullable: false),
                    icms_valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_ibs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_cbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    denegada = table.Column<bool>(type: "boolean", nullable: false),
                    pedido_venda_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendedor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    omie_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notas_fiscais", x => x.id);
                    table.ForeignKey(
                        name: "fk_notas_fiscais_clientes_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "clientes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notas_fiscais_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notas_fiscais_pedidos_venda_pedido_venda_id",
                        column: x => x.pedido_venda_id,
                        principalTable: "pedidos_venda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notas_fiscais_vendedores_vendedor_id",
                        column: x => x.vendedor_id,
                        principalTable: "vendedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "pedido_parcelas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pedido_venda_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_parcela = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    meio_pagamento_id = table.Column<Guid>(type: "uuid", nullable: true),
                    categoria = table.Column<string>(type: "text", nullable: true),
                    nsu = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pedido_parcelas", x => x.id);
                    table.ForeignKey(
                        name: "fk_pedido_parcelas_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_pedido_parcelas_meios_pagamento_meio_pagamento_id",
                        column: x => x.meio_pagamento_id,
                        principalTable: "meios_pagamento",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_pedido_parcelas_pedidos_venda_pedido_venda_id",
                        column: x => x.pedido_venda_id,
                        principalTable: "pedidos_venda",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "itens_nota_fiscal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nota_fiscal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    produto_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    valor_unitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    valor_total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    cfop = table.Column<string>(type: "text", nullable: true),
                    ncm = table.Column<string>(type: "text", nullable: true),
                    base_icms = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_icms = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_icms = table.Column<string>(type: "text", nullable: true),
                    valor_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    base_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_ipi = table.Column<decimal>(type: "numeric", nullable: false),
                    cst_ipi = table.Column<string>(type: "text", nullable: true),
                    valor_pis = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_cofins = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_ibs = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_ibs = table.Column<decimal>(type: "numeric", nullable: false),
                    valor_cbs = table.Column<decimal>(type: "numeric", nullable: false),
                    aliq_cbs = table.Column<decimal>(type: "numeric", nullable: false),
                    base_ibs_cbs = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itens_nota_fiscal", x => x.id);
                    table.ForeignKey(
                        name: "fk_itens_nota_fiscal_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_itens_nota_fiscal_produtos_produto_id",
                        column: x => x.produto_id,
                        principalTable: "produtos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nota_fiscal_titulos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nota_fiscal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    numero_parcela = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    data_vencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    omie_id_titulo = table.Column<long>(type: "bigint", nullable: true),
                    conta_corrente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendedor_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_nota_fiscal_titulos", x => x.id);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_titulos_contas_corrente_conta_corrente_id",
                        column: x => x.conta_corrente_id,
                        principalTable: "contas_corrente",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_titulos_notas_fiscais_nota_fiscal_id",
                        column: x => x.nota_fiscal_id,
                        principalTable: "notas_fiscais",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_nota_fiscal_titulos_vendedores_vendedor_id",
                        column: x => x.vendedor_id,
                        principalTable: "vendedores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bancos_codigo_banco",
                table: "bancos",
                column: "codigo_banco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_clientes_omie_id",
                table: "clientes",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_condicoes_pagamento_codigo",
                table: "condicoes_pagamento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contas_corrente_banco_id",
                table: "contas_corrente",
                column: "banco_id");

            migrationBuilder.CreateIndex(
                name: "ix_contas_corrente_omie_id",
                table: "contas_corrente",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_etapas_faturamento_codigo_operacao_codigo",
                table: "etapas_faturamento",
                columns: new[] { "codigo_operacao", "codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_formas_pagamento_codigo",
                table: "formas_pagamento",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itens_nota_fiscal_nota_fiscal_id",
                table: "itens_nota_fiscal",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_nota_fiscal_produto_id",
                table: "itens_nota_fiscal",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_pedido_pedido_venda_id",
                table: "itens_pedido",
                column: "pedido_venda_id");

            migrationBuilder.CreateIndex(
                name: "ix_itens_pedido_produto_id",
                table: "itens_pedido",
                column: "produto_id");

            migrationBuilder.CreateIndex(
                name: "ix_meios_pagamento_omie_id",
                table: "meios_pagamento",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_titulos_conta_corrente_id",
                table: "nota_fiscal_titulos",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_titulos_nota_fiscal_id",
                table: "nota_fiscal_titulos",
                column: "nota_fiscal_id");

            migrationBuilder.CreateIndex(
                name: "ix_nota_fiscal_titulos_vendedor_id",
                table: "nota_fiscal_titulos",
                column: "vendedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_cliente_id",
                table: "notas_fiscais",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_conta_corrente_id",
                table: "notas_fiscais",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_omie_id",
                table: "notas_fiscais",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_pedido_venda_id",
                table: "notas_fiscais",
                column: "pedido_venda_id");

            migrationBuilder.CreateIndex(
                name: "ix_notas_fiscais_vendedor_id",
                table: "notas_fiscais",
                column: "vendedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_parcelas_conta_corrente_id",
                table: "pedido_parcelas",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_parcelas_meio_pagamento_id",
                table: "pedido_parcelas",
                column: "meio_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedido_parcelas_pedido_venda_id",
                table: "pedido_parcelas",
                column: "pedido_venda_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_cliente_id",
                table: "pedidos_venda",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_condicao_pagamento_id",
                table: "pedidos_venda",
                column: "condicao_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_conta_corrente_id",
                table: "pedidos_venda",
                column: "conta_corrente_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_etapa_faturamento_id",
                table: "pedidos_venda",
                column: "etapa_faturamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_forma_pagamento_id",
                table: "pedidos_venda",
                column: "forma_pagamento_id");

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_omie_id",
                table: "pedidos_venda",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pedidos_venda_vendedor_id",
                table: "pedidos_venda",
                column: "vendedor_id");

            migrationBuilder.CreateIndex(
                name: "ix_perfis_telegram_chat_id",
                table: "perfis",
                column: "telegram_chat_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_perfis_telegram_link_token",
                table: "perfis",
                column: "telegram_link_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_produtos_omie_id",
                table: "produtos",
                column: "omie_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vendedores_omie_id",
                table: "vendedores",
                column: "omie_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_sync_states");

            migrationBuilder.DropTable(
                name: "itens_nota_fiscal");

            migrationBuilder.DropTable(
                name: "itens_pedido");

            migrationBuilder.DropTable(
                name: "logs");

            migrationBuilder.DropTable(
                name: "nota_fiscal_titulos");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "pedido_parcelas");

            migrationBuilder.DropTable(
                name: "perfis");

            migrationBuilder.DropTable(
                name: "sync_locks");

            migrationBuilder.DropTable(
                name: "webhook_events");

            migrationBuilder.DropTable(
                name: "produtos");

            migrationBuilder.DropTable(
                name: "notas_fiscais");

            migrationBuilder.DropTable(
                name: "meios_pagamento");

            migrationBuilder.DropTable(
                name: "pedidos_venda");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropTable(
                name: "condicoes_pagamento");

            migrationBuilder.DropTable(
                name: "contas_corrente");

            migrationBuilder.DropTable(
                name: "etapas_faturamento");

            migrationBuilder.DropTable(
                name: "formas_pagamento");

            migrationBuilder.DropTable(
                name: "vendedores");

            migrationBuilder.DropTable(
                name: "bancos");
        }
    }
}
