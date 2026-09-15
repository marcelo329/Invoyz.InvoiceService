using Invoyz.InvoiceService.Application.Data;
using Invoyz.InvoiceService.Contracts.InboundContracts.InvoiceLines;
using Invoyz.InvoiceService.Contracts.InboundContracts.Invoices;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Domains.Entities;
using Invoyz.InvoiceService.Tests.Boostrap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Invoyz.InvoiceService.Tests;

public class InvoiceControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    const string baseUrl = "/api/v1/Invoices";
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;

    public InvoiceControllerIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    #region Get

    [Fact]
    public async Task Get_WithPaging_ReturnsRequestedPageSize()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 12, cancellationToken);

        var response = await _client.GetAsync($"{baseUrl}?page=1&pageSize=5", cancellationToken);

        response.EnsureSuccessStatusCode();
        var invoices = await response.Content.ReadFromJsonAsync<InvoiceContract[]>(cancellationToken);

        Assert.NotNull(invoices);
        Assert.Equal(5, invoices.Length);
        Assert.All(invoices, invoice => Assert.NotEmpty(invoice.Lines));
    }

    [Fact]
    public async Task Get_ReturnsInvoicesCarryingTheirLinesAndTotals()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 3, cancellationToken);

        var invoices = await _client.GetFromJsonAsync<InvoiceContract[]>(baseUrl, cancellationToken);

        Assert.NotNull(invoices);
        Assert.All(invoices, invoice =>
        {
            Assert.Equal(invoice.Lines.Sum(line => line.LineTotal), invoice.SubTotal);
            Assert.Equal(invoice.Lines.Sum(line => line.LineTax), invoice.TaxTotal);
            Assert.Equal(invoice.SubTotal + invoice.TaxTotal, invoice.GrandTotal);
        });
    }

    #endregion

    #region Get by Id

    [Fact]
    public async Task GetById_WithExistingId_ReturnsInvoiceWithLines()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var expected = seed.Invoices[0];

        var response = await _client.GetAsync($"{baseUrl}/{expected.Id}", cancellationToken);

        response.EnsureSuccessStatusCode();
        var invoice = await response.Content.ReadFromJsonAsync<InvoiceContract>(cancellationToken);

        Assert.NotNull(invoice);
        Assert.Equal(expected.Id, invoice.Id);
        Assert.Equal(expected.InvoiceNumber, invoice.InvoiceNumber);
        Assert.Equal(expected.CustomerId, invoice.CustomerId);
        Assert.Equal(expected.Status, invoice.Status);
        Assert.NotEmpty(invoice.Lines);
        Assert.All(invoice.Lines, line => Assert.Equal(expected.Id, line.InvoiceId));
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.GetAsync($"{baseUrl}/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.GetAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST

    [Fact]
    public async Task Post_WithLines_PersistsInvoiceAndComputesTotals()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        // 2 x 100.00 @ 23%  +  3 x 50.00 @ 10%
        var payload = NewInvoiceContract(seed, "INV-POST-1",
        [
            new CreateInvoiceLineContract(seed.Products[0].Id, 2, 100m, 0.23m),
            new CreateInvoiceLineContract(seed.Products[1].Id, 3, 50m, 0.10m)
        ]);

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await GetByNumberAsync("INV-POST-1", cancellationToken);

        Assert.NotNull(created);
        Assert.Equal(2, created.Lines.Count);

        var first = created.Lines.Single(line => line.Quantity == 2);
        var second = created.Lines.Single(line => line.Quantity == 3);

        Assert.Equal(200m, first.LineTotal);
        Assert.Equal(46m, first.LineTax);
        Assert.Equal(150m, second.LineTotal);
        Assert.Equal(15m, second.LineTax);

        Assert.Equal(350m, created.SubTotal);
        Assert.Equal(61m, created.TaxTotal);
        Assert.Equal(411m, created.GrandTotal);

        #endregion
    }

    [Fact]
    public async Task Post_WithoutUnitPriceOrTaxRate_SnapshotsThemFromTheProduct()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);
        var product = seed.Products[0];

        var payload = NewInvoiceContract(seed, "INV-SNAPSHOT",
            [new CreateInvoiceLineContract(product.Id, 4)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await GetByNumberAsync("INV-SNAPSHOT", cancellationToken);

        Assert.NotNull(created);
        var line = Assert.Single(created.Lines);
        Assert.Equal(product.UnitPrice, line.UnitPrice);
        Assert.Equal(product.TaxRate, line.TaxRate);
    }

    [Fact]
    public async Task Post_WithNoLines_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.PostAsJsonAsync(
            baseUrl,
            NewInvoiceContract(seed, "INV-NOLINES", []),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithDuplicateInvoiceNumber_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var payload = NewInvoiceContract(seed, seed.Invoices[0].InvoiceNumber,
            [new CreateInvoiceLineContract(seed.Products[0].Id, 1)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownCustomer_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var payload = new CreateInvoiceContract(
            "INV-NOCUSTOMER",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            "Draft",
            [new CreateInvoiceLineContract(seed.Products[0].Id, 1)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithUnknownProduct_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var payload = NewInvoiceContract(seed, "INV-NOPRODUCT",
            [new CreateInvoiceLineContract(Guid.NewGuid(), 1)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Nonsense")]
    [InlineData("")]
    public async Task Post_WithInvalidStatus_ReturnsBadRequest(string status)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var payload = new CreateInvoiceContract(
            "INV-BADSTATUS",
            seed.Customers[0].Id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            status,
            [new CreateInvoiceLineContract(seed.Products[0].Id, 1)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithDueDateBeforeIssueDate_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var payload = new CreateInvoiceContract(
            "INV-BADDATES",
            seed.Customers[0].Id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(-1),
            "Draft",
            [new CreateInvoiceLineContract(seed.Products[0].Id, 1)]);

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_WithoutPayload_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 1, cancellationToken);

        using var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(baseUrl, emptyBody, cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT

    [Fact]
    public async Task Put_WithChangedStatus_PersistsIt()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var target = seed.Invoices[0];

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{target.Id}",
            new UpdateInvoiceContract(target.InvoiceNumber, target.CustomerId, target.IssueDate, target.DueDate, "Paid"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var persisted = await ReadInvoiceAsync(target.Id, cancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal("Paid", persisted.Status);
    }

    [Fact]
    public async Task Put_NormalisesStatusCasing()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var target = seed.Invoices[0];

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{target.Id}",
            new UpdateInvoiceContract(target.InvoiceNumber, target.CustomerId, target.IssueDate, target.DueDate, "oVeRdUe"),
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var persisted = await ReadInvoiceAsync(target.Id, cancellationToken);
        Assert.NotNull(persisted);
        Assert.Equal("Overdue", persisted.Status);
    }

    [Fact]
    public async Task Put_WithInvoiceNumberBelongingToAnotherInvoice_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var target = seed.Invoices[0];
        var other = seed.Invoices[1];

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{target.Id}",
            new UpdateInvoiceContract(other.InvoiceNumber, target.CustomerId, target.IssueDate, target.DueDate, "Sent"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_ForUnknownInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{Guid.NewGuid()}",
            new UpdateInvoiceContract("INV-GHOST", seed.Customers[0].Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), "Draft"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithNoChanges_ReturnsConflict()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);
        var target = seed.Invoices[0];

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{target.Id}",
            new UpdateInvoiceContract(target.InvoiceNumber, target.CustomerId, target.IssueDate, target.DueDate, target.Status),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Put_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.PutAsJsonAsync(
            $"{baseUrl}/{Guid.Empty}",
            new UpdateInvoiceContract("INV-EMPTY", seed.Customers[0].Id, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(5), "Draft"),
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region DELETE

    [Fact]
    public async Task Delete_ExistingInvoice_ReturnsNoContentAndCascadesToItsLines()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var target = seed.Invoices[0];

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{target.Id}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var readBack = await _client.GetAsync($"{baseUrl}/{target.Id}", cancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, readBack.StatusCode);

        var liveLines = await CountLiveLinesAsync(target.Id, cancellationToken);
        Assert.Equal(0, liveLines);

        #endregion
    }

    [Fact]
    public async Task Delete_FreesTheInvoiceNumberForReuse()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var seed = await SeedAsync(invoiceCount: 2, cancellationToken);
        var target = seed.Invoices[0];

        await _client.DeleteAsync($"{baseUrl}/{target.Id}", cancellationToken);

        var response = await _client.PostAsJsonAsync(
            baseUrl,
            NewInvoiceContract(seed, target.InvoiceNumber, [new CreateInvoiceLineContract(seed.Products[0].Id, 1)]),
            cancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistentInvoice_ReturnsNotFound()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.DeleteAsync($"{baseUrl}/{Guid.NewGuid()}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithEmptyId_ReturnsBadRequest()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedAsync(invoiceCount: 1, cancellationToken);

        var response = await _client.DeleteAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Helpers

    internal sealed record Seed(
        CustomerEntity[] Customers,
        ProductEntity[] Products,
        InvoiceEntity[] Invoices);

    private static CreateInvoiceContract NewInvoiceContract(
        Seed seed,
        string invoiceNumber,
        IReadOnlyCollection<CreateInvoiceLineContract> lines)
        => new(
            invoiceNumber,
            seed.Customers[0].Id,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(30),
            "Draft",
            lines);

    private async Task<Seed> SeedAsync(int invoiceCount, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await appDbContext.InvoiceLines.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Invoices.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Products.ExecuteDeleteAsync(cancellationToken);
        await appDbContext.Customers.ExecuteDeleteAsync(cancellationToken);

        var customers = Enumerable.Range(1, 3).Select(i => new CustomerEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Customer {i:D2}",
            Address = $"Address {i:D2}",
            Email = $"customer{i:D2}@invoyz.test",
            VatNumber = $"vat-{i:D2}",
            CreatedAt = DateTimeOffset.UtcNow
        }).ToArray();

        var products = Enumerable.Range(1, 3).Select(i => new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = $"Product {i:D2}",
            Description = $"Product {i:D2} description",
            UnitPrice = i * 10m,
            TaxRate = 0.23m,
            CreatedAt = DateTimeOffset.UtcNow
        }).ToArray();

        appDbContext.Customers.AddRange(customers);
        appDbContext.Products.AddRange(products);

        var invoices = new List<InvoiceEntity>();

        for (var i = 1; i <= invoiceCount; i++)
        {
            var product = products[i % products.Length];
            var quantity = i;

            var line = new InvoiceLineEntity
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.UnitPrice,
                TaxRate = product.TaxRate,
                LineTotal = quantity * product.UnitPrice,
                LineTax = decimal.Round(quantity * product.UnitPrice * product.TaxRate, 2, MidpointRounding.AwayFromZero),
                CreatedAt = DateTimeOffset.UtcNow
            };

            var invoice = new InvoiceEntity
            {
                Id = Guid.NewGuid(),
                InvoiceNumber = $"INV-{i:D4}",
                CustomerId = customers[i % customers.Length].Id,
                IssueDate = DateTimeOffset.UtcNow,
                DueDate = DateTimeOffset.UtcNow.AddDays(30),
                Status = "Draft",
                SubTotal = line.LineTotal,
                TaxTotal = line.LineTax,
                GrandTotal = line.LineTotal + line.LineTax,
                CreatedAt = DateTimeOffset.UtcNow,
                InvoiceLines = [line]
            };

            invoices.Add(invoice);
        }

        appDbContext.Invoices.AddRange(invoices);
        await appDbContext.SaveChangesAsync(cancellationToken);

        return new Seed(customers, products, [.. invoices]);
    }

    private async Task<InvoiceContract?> GetByNumberAsync(string invoiceNumber, CancellationToken cancellationToken)
    {
        var invoices = await _client.GetFromJsonAsync<InvoiceContract[]>(
            $"{baseUrl}?page=1&pageSize=100", cancellationToken);

        return invoices?.FirstOrDefault(invoice => invoice.InvoiceNumber == invoiceNumber);
    }

    private async Task<InvoiceEntity?> ReadInvoiceAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Invoices
            .AsNoTracking()
            .FirstOrDefaultAsync(invoice => invoice.Id == id, cancellationToken);
    }

    private async Task<int> CountLiveLinesAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.InvoiceLines
            .AsNoTracking()
            .CountAsync(line => line.InvoiceId == invoiceId && !line.IsDeleted, cancellationToken);
    }

    #endregion
}
