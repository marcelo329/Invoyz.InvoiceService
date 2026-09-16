using Invoyz.InvoiceService.Application.Data;
using Invoyz.InvoiceService.Contracts.InboundContracts.InvoiceLines;
using Invoyz.InvoiceService.Contracts.RestAPI.OutboundContracts;
using Invoyz.InvoiceService.Domains.Entities;
using Invoyz.InvoiceService.Tests.Boostrap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Invoyz.InvoiceService.Tests;

public class InvoiceLineControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public InvoiceLineControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    private static string LinesUrl(Guid invoiceId) => $"/api/v1/Invoices/{invoiceId}/Lines";

    #region Get

    [Fact]
    public async Task Get_ReturnsOnlyTheLinesOfTheAddressedInvoice()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var lines = await _client.GetFromJsonAsync<InvoiceLineContract[]>(
            LinesUrl(seed.FirstInvoiceId), cancellationToken);

        Assert.NotNull(lines);
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Equal(seed.FirstInvoiceId, line.InvoiceId));
    }

    [Fact]
    public async Task GetById_WithExistingLine_ReturnsIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.GetAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        response.EnsureSuccessStatusCode();
        var line = await response.Content.ReadFromJsonAsync<InvoiceLineContract>(cancellationToken);

        Assert.NotNull(line);
        Assert.Equal(seed.FirstLineId, line.Id);
        Assert.Equal(seed.FirstInvoiceId, line.InvoiceId);
    }

    // A line id is only meaningful inside its own invoice: addressing it through a
    // different invoice must read as missing, never as that other invoice data.
    [Fact]
    public async Task GetById_ThroughTheWrongInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.GetAsync(
            $"{LinesUrl(seed.SecondInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithEmptyLineId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.GetAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST

    [Fact]
    public async Task Post_AddsTheLineAndRecalculatesTheInvoiceTotals()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var before = await ReadInvoiceAsync(seed.FirstInvoiceId, cancellationToken);
        Assert.NotNull(before);

        #endregion

        // 3 x 100.00 @ 23% = 300.00 subtotal, 69.00 tax
        var response = await _client.PostAsJsonAsync(
            LinesUrl(seed.FirstInvoiceId),
            new CreateInvoiceLineContract(seed.ProductId, 3, 100m, 0.23m),
            cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var after = await ReadInvoiceAsync(seed.FirstInvoiceId, cancellationToken);

        Assert.NotNull(after);
        Assert.Equal(before.SubTotal + 300m, after.SubTotal);
        Assert.Equal(before.TaxTotal + 69m, after.TaxTotal);
        Assert.Equal(after.SubTotal + after.TaxTotal, after.GrandTotal);

        #endregion
    }

    [Fact]
    public async Task Post_WithoutPriceOrRate_SnapshotsThemFromTheProduct()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PostAsJsonAsync(
            LinesUrl(seed.FirstInvoiceId),
            new CreateInvoiceLineContract(seed.ProductId, 2),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var lines = await _client.GetFromJsonAsync<InvoiceLineContract[]>(
            LinesUrl(seed.FirstInvoiceId), cancellationToken);

        var added = Assert.Single(lines!, line => line.Quantity == 2);
        Assert.Equal(seed.ProductUnitPrice, added.UnitPrice);
        Assert.Equal(seed.ProductTaxRate, added.TaxRate);
    }

    [Fact]
    public async Task Post_ToUnknownInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PostAsJsonAsync(
            LinesUrl(Guid.NewGuid()),
            new CreateInvoiceLineContract(seed.ProductId, 1),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownProduct_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PostAsJsonAsync(
            LinesUrl(seed.FirstInvoiceId),
            new CreateInvoiceLineContract(Guid.NewGuid(), 1),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Post_WithNonPositiveQuantity_ReturnsBadRequest(int quantity)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PostAsJsonAsync(
            LinesUrl(seed.FirstInvoiceId),
            new CreateInvoiceLineContract(seed.ProductId, quantity),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutPayload_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        using var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(LinesUrl(seed.FirstInvoiceId), emptyBody, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT

    [Fact]
    public async Task Put_ChangingQuantity_RecomputesLineAndInvoiceTotals()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        #endregion

        // 5 x 100.00 @ 23% = 500.00 line total, 115.00 line tax
        var response = await _client.PutAsJsonAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}",
            new UpdateInvoiceLineContract(seed.ProductId, 5, 100m, 0.23m),
            cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();

        var line = await _client.GetFromJsonAsync<InvoiceLineContract>(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        Assert.NotNull(line);
        Assert.Equal(500m, line.LineTotal);
        Assert.Equal(115m, line.LineTax);

        var invoice = await ReadInvoiceAsync(seed.FirstInvoiceId, cancellationToken);

        Assert.NotNull(invoice);
        Assert.Equal(500m, invoice.SubTotal);
        Assert.Equal(115m, invoice.TaxTotal);
        Assert.Equal(615m, invoice.GrandTotal);

        #endregion
    }

    [Fact]
    public async Task Put_ThroughTheWrongInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{LinesUrl(seed.SecondInvoiceId)}/{seed.FirstLineId}",
            new UpdateInvoiceLineContract(seed.ProductId, 9, 1m, 0.1m),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithNoChanges_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var existing = await _client.GetFromJsonAsync<InvoiceLineContract>(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        Assert.NotNull(existing);

        var response = await _client.PutAsJsonAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}",
            new UpdateInvoiceLineContract(existing.ProductId, existing.Quantity, existing.UnitPrice, existing.TaxRate),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithEmptyLineId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{Guid.Empty}",
            new UpdateInvoiceLineContract(seed.ProductId, 1, 1m, 0.1m),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DELETE

    [Fact]
    public async Task Delete_RemovesTheLineAndRecalculatesTheInvoiceTotals()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        // Second line so the invoice is not left empty by the delete.
        await _client.PostAsJsonAsync(
            LinesUrl(seed.FirstInvoiceId),
            new CreateInvoiceLineContract(seed.ProductId, 2, 100m, 0.23m),
            cancellationToken);

        #endregion

        var response = await _client.DeleteAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var invoice = await ReadInvoiceAsync(seed.FirstInvoiceId, cancellationToken);

        Assert.NotNull(invoice);
        Assert.Equal(200m, invoice.SubTotal);
        Assert.Equal(46m, invoice.TaxTotal);
        Assert.Equal(246m, invoice.GrandTotal);

        var readBack = await _client.GetAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, readBack.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Delete_TheLastRemainingLine_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.DeleteAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ThroughTheWrongInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.DeleteAsync(
            $"{LinesUrl(seed.SecondInvoiceId)}/{seed.FirstLineId}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithEmptyLineId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(cancellationToken);

        var response = await _client.DeleteAsync(
            $"{LinesUrl(seed.FirstInvoiceId)}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helpers

    internal sealed record Seed(
        Guid FirstInvoiceId,
        Guid SecondInvoiceId,
        Guid FirstLineId,
        Guid ProductId,
        decimal ProductUnitPrice,
        decimal ProductTaxRate);

    private async Task<Seed> SeedAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await appDbContext.InvoiceLines.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Invoices.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Products.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Customers.ExecuteDeleteAsync(cancellationToken);

        var customer = new CustomerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Line customer",
            Address = "Line street",
            Email = "lines@invoyz.test",
            VatNumber = "vat-lines",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var product = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Line product",
            Description = "Line product description",
            UnitPrice = 100m,
            TaxRate = 0.23m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var firstLine = new InvoiceLineEntity
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 0.23m,
            LineTotal = 100m,
            LineTax = 23m,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var firstInvoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-LINES-1",
            CustomerId = customer.Id,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Status = "Draft",
            SubTotal = 100m,
            TaxTotal = 23m,
            GrandTotal = 123m,
            CreatedAt = DateTimeOffset.UtcNow,
            InvoiceLines = [firstLine]
        };

        var secondInvoice = new InvoiceEntity
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = "INV-LINES-2",
            CustomerId = customer.Id,
            IssueDate = DateTimeOffset.UtcNow,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            Status = "Draft",
            SubTotal = 0m,
            TaxTotal = 0m,
            GrandTotal = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
            InvoiceLines = []
        };

        appDbContext.Customers.Add(customer);
        appDbContext.Products.Add(product);
        appDbContext.Invoices.AddRange(firstInvoice, secondInvoice);

        await appDbContext.SaveChangesAsync(cancellationToken);

        return new Seed(
            firstInvoice.Id,
            secondInvoice.Id,
            firstLine.Id,
            product.Id,
            product.UnitPrice,
            product.TaxRate);
    }

    private async Task<InvoiceEntity?> ReadInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    #endregion
}
