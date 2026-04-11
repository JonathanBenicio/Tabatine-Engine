global using Xunit;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.EntityFrameworkCore;
global using NSubstitute;
global using FluentAssertions;
global using Tabatine.Core.Entities;
global using Tabatine.Core.Interfaces;
global using Tabatine.Infrastructure.Data;
global using Tabatine.Omie.Client;
global using Tabatine.Omie.Client.Models;
global using Tabatine.Omie.Client.Models.Produtos;
global using Tabatine.Omie.Client.Models.Pedidos;
global using Tabatine.Omie.Client.Models.Clientes;
global using System.Net.Http.Json;
global using System.Text.Json;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
