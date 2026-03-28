using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tabatine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "Bancos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoBanco = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CodigoIspb = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bancos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazaoSocial = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeFantasia = table.Column<string>(type: "text", nullable: false),
                    CnpjCpf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    InscricaoEstadual = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OptanteSimplesNacional = table.Column<bool>(type: "boolean", nullable: false),
                    Cep = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Cidade = table.Column<string>(type: "text", nullable: true),
                    Endereco = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    EnderecoNumero = table.Column<string>(type: "text", nullable: true),
                    EnderecoComplemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CondicoesPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    DiaFixo = table.Column<int>(type: "integer", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CondicoesPagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EtapasFaturamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DescricaoPadrao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Inativa = table.Column<bool>(type: "boolean", nullable: false),
                    CodigoOperacao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DescricaoOperacao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EtapasFaturamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormasPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    DiasParcelas = table.Column<int>(type: "integer", nullable: true),
                    ListaParcelas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormasPagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationSyncStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationSyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Message = table.Column<string>(type: "text", nullable: true),
                    MessageTemplate = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Exception = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    LogEvent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeiosPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeiosPagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    reference_id = table.Column<long>(type: "bigint", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoProduto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Ncm = table.Column<string>(type: "text", nullable: false),
                    Ean = table.Column<string>(type: "text", nullable: true),
                    UnidadeMedida = table.Column<string>(type: "text", nullable: true),
                    PesoLiquido = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PesoBruto = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    FamiliaProduto = table.Column<string>(type: "text", nullable: true),
                    PrecoUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncLocks",
                columns: table => new
                {
                    LockKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LockToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLocks", x => x.LockKey);
                });

            migrationBuilder.CreateTable(
                name: "Vendedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Comissao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Inativo = table.Column<bool>(type: "boolean", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Event = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContasCorrente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CodigoIntegracao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Inativa = table.Column<bool>(type: "boolean", nullable: false),
                    BancoId = table.Column<Guid>(type: "uuid", nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContasCorrente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContasCorrente_Bancos_BancoId",
                        column: x => x.BancoId,
                        principalTable: "Bancos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PedidosVenda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroPedido = table.Column<string>(type: "text", nullable: false),
                    Etapa = table.Column<string>(type: "text", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataPrevisao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorFrete = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Transportadora = table.Column<string>(type: "text", nullable: true),
                    QuantidadeVolumes = table.Column<int>(type: "integer", nullable: false),
                    PesoBruto = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PesoLiquido = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PrevisaoEntrega = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseCalculoIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorMercadorias = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIss = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorIr = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCsll = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorInss = table.Column<decimal>(type: "numeric", nullable: false),
                    ComissaoVendedor = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    FreteModalidade = table.Column<string>(type: "text", nullable: true),
                    CodigoParcela = table.Column<string>(type: "text", nullable: true),
                    Contato = table.Column<string>(type: "text", nullable: true),
                    ObservacoesVenda = table.Column<string>(type: "text", nullable: true),
                    ObservacoesInternas = table.Column<string>(type: "text", nullable: true),
                    DadosAdicionaisNf = table.Column<string>(type: "text", nullable: true),
                    MeioPagamento = table.Column<string>(type: "text", nullable: true),
                    QuantidadeParcelas = table.Column<int>(type: "integer", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CodigoRastreio = table.Column<string>(type: "text", nullable: true),
                    LinkRastreio = table.Column<string>(type: "text", nullable: true),
                    VeiculoProprio = table.Column<string>(type: "text", nullable: true),
                    Placa = table.Column<string>(type: "text", nullable: true),
                    ValorSeguro = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorOutrasDespesas = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NumeroPedidoCliente = table.Column<string>(type: "text", nullable: true),
                    ConsumidorFinal = table.Column<string>(type: "text", nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAlteracao = table.Column<string>(type: "text", nullable: true),
                    Faturado = table.Column<bool>(type: "boolean", nullable: false),
                    Cancelado = table.Column<bool>(type: "boolean", nullable: false),
                    Devolvido = table.Column<bool>(type: "boolean", nullable: false),
                    Autorizado = table.Column<bool>(type: "boolean", nullable: false),
                    Denegado = table.Column<bool>(type: "boolean", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    EtapaFaturamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FormaPagamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CondicaoPagamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContaCorrenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidosVenda", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_CondicoesPagamento_CondicaoPagamentoId",
                        column: x => x.CondicaoPagamentoId,
                        principalTable: "CondicoesPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_ContasCorrente_ContaCorrenteId",
                        column: x => x.ContaCorrenteId,
                        principalTable: "ContasCorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_EtapasFaturamento_EtapaFaturamentoId",
                        column: x => x.EtapaFaturamentoId,
                        principalTable: "EtapasFaturamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_FormasPagamento_FormaPagamentoId",
                        column: x => x.FormaPagamentoId,
                        principalTable: "FormasPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidosVenda_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItensPedido",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<int>(type: "integer", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnidadeMedida = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Cfop = table.Column<string>(type: "text", nullable: true),
                    ValorIcms = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIpi = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PercentualDesconto = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseIcms = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIcms = table.Column<decimal>(type: "numeric", nullable: false),
                    CstIcms = table.Column<string>(type: "text", nullable: true),
                    BaseIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    CstIpi = table.Column<string>(type: "text", nullable: true),
                    BasePis = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqPis = table.Column<decimal>(type: "numeric", nullable: false),
                    CstPis = table.Column<string>(type: "text", nullable: true),
                    BaseCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    CstCofins = table.Column<string>(type: "text", nullable: true),
                    PesoBruto = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    PesoLiquido = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ValorIbs = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIbs = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCbs = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqCbs = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseIbsCbs = table.Column<decimal>(type: "numeric", nullable: false),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensPedido", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensPedido_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensPedido_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotasFiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroNf = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChaveAcesso = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CodigoStatus = table.Column<int>(type: "integer", nullable: false),
                    DataEmissao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HoraEmissao = table.Column<TimeSpan>(type: "interval", nullable: true),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NaturezaOperacao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Serie = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Modelo = table.Column<string>(type: "text", nullable: true),
                    ImportadoApi = table.Column<bool>(type: "boolean", nullable: false),
                    DataSaida = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HoraSaida = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IdTransportadora = table.Column<long>(type: "bigint", nullable: true),
                    ValorIss = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIr = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCsll = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorPisRetido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCofinsRetido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorFrete = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorSeguro = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorOutrasDespesas = table.Column<decimal>(type: "numeric", nullable: false),
                    IssqnBaseCalculo = table.Column<decimal>(type: "numeric", nullable: false),
                    TipoOperacao = table.Column<string>(type: "text", nullable: true),
                    Finalidade = table.Column<string>(type: "text", nullable: true),
                    Ambiente = table.Column<string>(type: "text", nullable: true),
                    InformacoesComplementares = table.Column<string>(type: "text", nullable: true),
                    InformacoesFisco = table.Column<string>(type: "text", nullable: true),
                    ValorIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorPis = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorProd = table.Column<decimal>(type: "numeric", nullable: false),
                    IcmsBaseCalculo = table.Column<decimal>(type: "numeric", nullable: false),
                    IcmsValor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorIbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorCbs = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Denegada = table.Column<bool>(type: "boolean", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContaCorrenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    OmieId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFiscais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_ContasCorrente_ContaCorrenteId",
                        column: x => x.ContaCorrenteId,
                        principalTable: "ContasCorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_NotasFiscais_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PedidoParcelas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PedidoVendaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroParcela = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    ContaCorrenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeioPagamentoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Categoria = table.Column<string>(type: "text", nullable: true),
                    Nsu = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PedidoParcelas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PedidoParcelas_ContasCorrente_ContaCorrenteId",
                        column: x => x.ContaCorrenteId,
                        principalTable: "ContasCorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PedidoParcelas_MeiosPagamento_MeioPagamentoId",
                        column: x => x.MeioPagamentoId,
                        principalTable: "MeiosPagamento",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PedidoParcelas_PedidosVenda_PedidoVendaId",
                        column: x => x.PedidoVendaId,
                        principalTable: "PedidosVenda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItensNotaFiscal",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantidade = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ValorUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Cfop = table.Column<string>(type: "text", nullable: true),
                    Ncm = table.Column<string>(type: "text", nullable: true),
                    BaseIcms = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIcms = table.Column<decimal>(type: "numeric", nullable: false),
                    CstIcms = table.Column<string>(type: "text", nullable: true),
                    ValorIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIpi = table.Column<decimal>(type: "numeric", nullable: false),
                    CstIpi = table.Column<string>(type: "text", nullable: true),
                    ValorPis = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCofins = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorIbs = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqIbs = table.Column<decimal>(type: "numeric", nullable: false),
                    ValorCbs = table.Column<decimal>(type: "numeric", nullable: false),
                    AliqCbs = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseIbsCbs = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensNotaFiscal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensNotaFiscal_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItensNotaFiscal_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotaFiscalTitulos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NotaFiscalId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroParcela = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DataVencimento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OmieIdTitulo = table.Column<long>(type: "bigint", nullable: true),
                    ContaCorrenteId = table.Column<Guid>(type: "uuid", nullable: true),
                    VendedorId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotaFiscalTitulos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotaFiscalTitulos_ContasCorrente_ContaCorrenteId",
                        column: x => x.ContaCorrenteId,
                        principalTable: "ContasCorrente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotaFiscalTitulos_NotasFiscais_NotaFiscalId",
                        column: x => x.NotaFiscalId,
                        principalTable: "NotasFiscais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NotaFiscalTitulos_Vendedores_VendedorId",
                        column: x => x.VendedorId,
                        principalTable: "Vendedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bancos_CodigoBanco",
                table: "Bancos",
                column: "CodigoBanco",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_OmieId",
                table: "Clientes",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CondicoesPagamento_Codigo",
                table: "CondicoesPagamento",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContasCorrente_BancoId",
                table: "ContasCorrente",
                column: "BancoId");

            migrationBuilder.CreateIndex(
                name: "IX_ContasCorrente_OmieId",
                table: "ContasCorrente",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EtapasFaturamento_CodigoOperacao_Codigo",
                table: "EtapasFaturamento",
                columns: new[] { "CodigoOperacao", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Codigo",
                table: "FormasPagamento",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensNotaFiscal_NotaFiscalId",
                table: "ItensNotaFiscal",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensNotaFiscal_ProdutoId",
                table: "ItensNotaFiscal",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_PedidoVendaId",
                table: "ItensPedido",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensPedido_ProdutoId",
                table: "ItensPedido",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_MeiosPagamento_OmieId",
                table: "MeiosPagamento",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_ContaCorrenteId",
                table: "NotaFiscalTitulos",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_NotaFiscalId",
                table: "NotaFiscalTitulos",
                column: "NotaFiscalId");

            migrationBuilder.CreateIndex(
                name: "IX_NotaFiscalTitulos_VendedorId",
                table: "NotaFiscalTitulos",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ClienteId",
                table: "NotasFiscais",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_ContaCorrenteId",
                table: "NotasFiscais",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_OmieId",
                table: "NotasFiscais",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_PedidoVendaId",
                table: "NotasFiscais",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFiscais_VendedorId",
                table: "NotasFiscais",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_ContaCorrenteId",
                table: "PedidoParcelas",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_MeioPagamentoId",
                table: "PedidoParcelas",
                column: "MeioPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidoParcelas_PedidoVendaId",
                table: "PedidoParcelas",
                column: "PedidoVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_ClienteId",
                table: "PedidosVenda",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_CondicaoPagamentoId",
                table: "PedidosVenda",
                column: "CondicaoPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_ContaCorrenteId",
                table: "PedidosVenda",
                column: "ContaCorrenteId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_EtapaFaturamentoId",
                table: "PedidosVenda",
                column: "EtapaFaturamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_FormaPagamentoId",
                table: "PedidosVenda",
                column: "FormaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_OmieId",
                table: "PedidosVenda",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PedidosVenda_VendedorId",
                table: "PedidosVenda",
                column: "VendedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_OmieId",
                table: "Produtos",
                column: "OmieId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vendedores_OmieId",
                table: "Vendedores",
                column: "OmieId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IntegrationSyncStates");

            migrationBuilder.DropTable(
                name: "ItensNotaFiscal");

            migrationBuilder.DropTable(
                name: "ItensPedido");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "NotaFiscalTitulos");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PedidoParcelas");

            migrationBuilder.DropTable(
                name: "SyncLocks");

            migrationBuilder.DropTable(
                name: "WebhookEvents");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "NotasFiscais");

            migrationBuilder.DropTable(
                name: "MeiosPagamento");

            migrationBuilder.DropTable(
                name: "PedidosVenda");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "CondicoesPagamento");

            migrationBuilder.DropTable(
                name: "ContasCorrente");

            migrationBuilder.DropTable(
                name: "EtapasFaturamento");

            migrationBuilder.DropTable(
                name: "FormasPagamento");

            migrationBuilder.DropTable(
                name: "Vendedores");

            migrationBuilder.DropTable(
                name: "Bancos");
        }
    }
}
