using Invoyz.InvoiceService.Application.Data;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Contracts.InboundContracts.Products;
using Invoyz.InvoiceService.Domains.Entities;
using Invoyz.InvoiceService.Tests.Boostrap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Invoyz.InvoiceService.Tests;

public class ProductControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    const string baseUrl = "/api/v1/Products";
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    private static readonly string[] SeededNames =
        [.. Enumerable.Range(1, 20).Select(i => $"Product {i:D2}")];

    public ProductControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Get

    [Fact]
    public async Task Get_WithPageOne_ReturnsRequestedPageSizeFromTheSeededSet()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        #endregion

        var response = await _client.GetAsync($"{baseUrl}?page=1&pageSize=10", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<ProductContract[]>(cancellationToken);

        Assert.NotNull(products);
        Assert.Equal(10, products.Length);
        Assert.Equal(10, products.Select(p => p.Id).Distinct().Count());
        Assert.All(products, p => Assert.Contains(p.Name, SeededNames));

        #endregion
    }

    // Order is deliberately not asserted: GetListAsync pages before the handler sorts,
    // so global ordering across pages is undefined. Asserting it either way would bake
    // that behaviour into the suite. See "Known gaps" in CLAUDE.md.
    [Fact]
    public async Task Get_WithPageTwo_ReturnsADifferentPageOfTheSameSize()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        #endregion

        var firstPage = await _client.GetFromJsonAsync<ProductContract[]>(
            $"{baseUrl}?page=1&pageSize=10", cancellationToken);
        var secondPage = await _client.GetFromJsonAsync<ProductContract[]>(
            $"{baseUrl}?page=2&pageSize=10", cancellationToken);

        #region Assert

        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(10, secondPage.Length);
        Assert.Empty(firstPage.Select(p => p.Id).Intersect(secondPage.Select(p => p.Id)));

        #endregion
    }

    [Fact]
    public async Task Get_WithNoParameters_UsesTheDefaultPageSize()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        #endregion

        var response = await _client.GetAsync(baseUrl, cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<ProductContract[]>(cancellationToken);

        Assert.NotNull(products);
        Assert.Equal(10, products.Length);

        #endregion
    }

    #endregion

    #region Get by Id

    [Fact]
    public async Task GetById_WithExistingId_ReturnsFullyFilledContract()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyProductsAsync(cancellationToken);

        var expected = fullSample[Random.Shared.Next(fullSample.Length)];

        #endregion

        var response = await _client.GetAsync($"{baseUrl}/{expected.Id}", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductContract>(cancellationToken);

        Assert.NotNull(product);
        Assert.Equal(expected.Id, product.Id);
        Assert.Equal(expected.Name, product.Name);
        Assert.Equal(expected.Description, product.Description);
        Assert.Equal(expected.UnitPrice, product.UnitPrice);
        Assert.Equal(expected.TaxRate, product.TaxRate);
        Assert.Equal(expected.CreatedAt, product.CreatedAt);
        Assert.Equal(expected.LastModifiedAt, product.LastModifiedAt);

        #endregion
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.GetAsync($"{baseUrl}/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.GetAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST

    [Fact]
    public async Task Post_WithValidPayload_PersistsEveryProperty()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var payload = new CreateProductContract("Widget", "A widget", 19.99m, 0.23m);

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var persisted = await ReadProductByNameAsync(payload.Name, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(payload.Description, persisted.Description);
        Assert.Equal(payload.UnitPrice, persisted.UnitPrice);
        Assert.Equal(payload.TaxRate, persisted.TaxRate);

        #endregion
    }

    [Theory]
    [InlineData("", "desc", 1, 0.1)]
    [InlineData("name", "", 1, 0.1)]
    [InlineData("name", "desc", -1, 0.1)]
    [InlineData("name", "desc", 1, 1.5)]
    public async Task Post_WithInvalidField_ReturnsBadRequest(
        string name, string description, decimal unitPrice, decimal taxRate)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.PostAsJsonAsync(
            baseUrl,
            new CreateProductContract(name, description, unitPrice, taxRate),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutPayload_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        using var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(baseUrl, emptyBody, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT

    [Theory]
    [InlineData(nameof(ProductContract.Name))]
    [InlineData(nameof(ProductContract.Description))]
    [InlineData(nameof(ProductContract.UnitPrice))]
    [InlineData(nameof(ProductContract.TaxRate))]
    public async Task Put_ChangingOneFieldAtATime_PersistsThatChange(string field)
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyProductsAsync(cancellationToken);
        var target = fullSample[0];

        var payload = new UpdateProductContract(
            field == nameof(ProductContract.Name) ? "Changed name" : target.Name,
            field == nameof(ProductContract.Description) ? "Changed description" : target.Description,
            field == nameof(ProductContract.UnitPrice) ? target.UnitPrice + 5m : target.UnitPrice,
            field == nameof(ProductContract.TaxRate) ? 0.42m : target.TaxRate);

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{target.Id}", payload, cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();

        var persisted = await ReadProductAsync(target.Id, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(payload.Name, persisted.Name);
        Assert.Equal(payload.Description, persisted.Description);
        Assert.Equal(payload.UnitPrice, persisted.UnitPrice);
        Assert.Equal(payload.TaxRate, persisted.TaxRate);

        #endregion
    }

    [Fact]
    public async Task Put_ForUnknownProduct_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{Guid.NewGuid()}",
            new UpdateProductContract("Ghost", "Ghost", 1m, 0.1m),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{Guid.Empty}",
            new UpdateProductContract("Empty", "Empty", 1m, 0.1m),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithNoChanges_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyProductsAsync(cancellationToken);
        var target = fullSample[0];

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{target.Id}",
            new UpdateProductContract(target.Name, target.Description, target.UnitPrice, target.TaxRate),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region DELETE

    [Fact]
    public async Task Delete_ExistingProduct_ReturnsNoContentAndRemovesItFromReads()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyProductsAsync(cancellationToken);
        var target = fullSample[0];

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{target.Id}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var readBack = await _client.GetAsync($"{baseUrl}/{target.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, readBack.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Delete_ProductReferencedByAnInvoiceLine_ReturnsConflict()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyProductsAsync(cancellationToken);
        var target = fullSample[0];

        await AttachInvoiceLineToAsync(target, cancellationToken);

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{target.Id}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var readBack = await _client.GetAsync($"{baseUrl}/{target.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.OK, readBack.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Delete_NonExistentProduct_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.DeleteAsync($"{baseUrl}/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyProductsAsync(cancellationToken);

        var response = await _client.DeleteAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helpers

    private async Task<ProductEntity[]> SeedTwentyProductsAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await appDbContext.InvoiceLines.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Invoices.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Products.ExecuteDeleteAsync(cancellationToken);

        var priceSeed = 1m;

        foreach (var name in SeededNames.Reverse())
        {
            appDbContext.Products.Add(new ProductEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = $"{name} description",
                UnitPrice = priceSeed++,
                TaxRate = 0.23m,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        return [.. appDbContext.Products];
    }

    // Gives the product a live invoice line so the delete guard has something to find.
    private async Task AttachInvoiceLineToAsync(ProductEntity product, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var customer = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Line owner",
            Address = "Somewhere",
            Email = "line.owner@invoyz.test",
            VatNumber = $"vat-{Guid.NewGuid():N}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var invoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = $"INV-{Guid.NewGuid():N}",
            CustomerId = customer.Id,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Status = "Draft",
            CreatedAt = DateTimeOffset.UtcNow
        };

        appDbContext.Customers.Add(customer);
        appDbContext.Invoices.Add(invoice);
        appDbContext.InvoiceLines.Add(new InvoiceLineEntity
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = product.UnitPrice,
            TaxRate = product.TaxRate,
            LineTotal = product.UnitPrice,
            LineTax = product.UnitPrice * product.TaxRate,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await appDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProductEntity?> ReadProductAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    private async Task<ProductEntity?> ReadProductByNameAsync(string name, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
    }

    #endregion
}
