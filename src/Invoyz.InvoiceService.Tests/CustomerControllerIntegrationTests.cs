using Invoyz.InvoiceService.Application.Data;
using Invoyz.InvoiceService.Application.Helpers;
using Invoyz.InvoiceService.Contracts.InboundContracts.OutboundContracts;
using Invoyz.InvoiceService.Domains.Entities;
using Invoyz.InvoiceService.Tests.Boostrap;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Invoyz.InvoiceService.Contracts.InboundContracts.Customers;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Invoyz.InvoiceService.Tests;

public class CustomerControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    const string baseUrl = "/api/v1/Customers";
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program>
        _factory;

    // "Customer 01" .. "Customer 20": zero padded so alphabetical order is also numeric order.
    private static readonly string[] ExpectedOrder =
        [.. Enumerable.Range(1, 20).Select(i => $"Customer {i:D2}")];

    public CustomerControllerIntegrationTests(
        CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // The fixture is shared across the class, so each test starts from a known table.
    // Rows are inserted in reverse alphabetical order: paging must not depend on
    // insertion order to produce a name-ordered result.
    private async Task<CustomerEntity[]> SeedTwentyCustomersAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await appDbContext.Customers.ExecuteDeleteAsync(cancellationToken);

        foreach (var name in ExpectedOrder.Reverse())
        {
            var slug = name.Replace(" ", string.Empty).ToLowerInvariant();

            appDbContext.Customers.Add(new CustomerEntity
            {
                Id = Guid.NewGuid(),
                Name = name,
                Address = $"{name} Street",
                Email = $"{slug}@invoyz.test",
                VatNumber = slug,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await appDbContext.SaveChangesAsync(cancellationToken);

        return appDbContext.Customers.ToArray();
    }

    #region Get

    [Fact]
    public async Task Get_WithPageOne_ReturnsFirstTenCustomersOrderedByName()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var controlledSample = fullSample
            .Take(10)
            .OrderBy(a => a.Name)
            .Select(a =>
                a.ToCustomerContract())
            .ToArray();

        #endregion

        var response = await _client.GetAsync($"{baseUrl}?page=1&pageSize=10", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var customers = await response.Content
            .ReadFromJsonAsync<CustomerContract[]>(cancellationToken);

        Assert.NotNull(customers);
        Assert.Equal(controlledSample, customers);

        #endregion
    }

    [Fact]
    public async Task Get_WithPageTwo_SkipsFirstTenAndReturnsLastFiveCustomersOrderedByName()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var controlledSample = fullSample
            .Skip(5)
            .Take(5)
            .OrderBy(a => a.Name)
            .Select(a =>
                a.ToCustomerContract())
            .ToArray();

        #endregion

        var response = await _client.GetAsync($"{baseUrl}?page=2&pageSize=5", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var customers = await response.Content
            .ReadFromJsonAsync<CustomerContract[]>(cancellationToken);

        Assert.NotNull(customers);
        Assert.Equal(controlledSample, customers);

        #endregion
    }

    [Fact]
    public async Task Get_NoParameters()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var controlledSample = fullSample
            .Skip(0)
            .Take(10)
            .OrderBy(a => a.Name)
            .Select(a =>
                a.ToCustomerContract())
            .ToArray();

        #endregion

        var response = await _client.GetAsync($"{baseUrl}", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var customers = await response.Content
            .ReadFromJsonAsync<CustomerContract[]>(cancellationToken);

        Assert.NotNull(customers);
        Assert.Equal(controlledSample, customers);

        #endregion
    }

    #endregion

    #region Get by Id


    [Fact]
    public async Task GetById_WithExistingId_ReturnsFullyFilledContract()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var expected = fullSample[Random.Shared.Next(fullSample.Length)];

        #endregion

        var response = await _client.GetAsync($"{baseUrl}/{expected.Id}", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();
        var customer = await response.Content
            .ReadFromJsonAsync<CustomerContract>(cancellationToken);

        Assert.NotNull(customer);

        Assert.Equal(expected.Name, customer.Name);
        Assert.Equal(expected.Address, customer.Address);
        Assert.Equal(expected.Email, customer.Email);
        Assert.Equal(expected.VatNumber, customer.VatNumber);
        Assert.Equal(expected.CreatedAt, customer.CreatedAt);
        Assert.Equal(expected.LastModifiedAt, customer.LastModifiedAt);

        // "Fully filled": no field left at its default by the mapping.
        Assert.False(string.IsNullOrWhiteSpace(customer.Name));
        Assert.False(string.IsNullOrWhiteSpace(customer.Address));
        Assert.False(string.IsNullOrWhiteSpace(customer.Email));
        Assert.False(string.IsNullOrWhiteSpace(customer.VatNumber));
        Assert.NotEqual(default, customer.CreatedAt);

        #endregion
    }

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNotFound()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var unknownId = Guid.NewGuid();

        #endregion

        var response = await _client.GetAsync($"{baseUrl}/{unknownId}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task GetById_WithEmptyId_ReturnsBadRequest()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        #endregion

        var response = await _client.GetAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        #endregion
    }


    #endregion

    #region POST

    [Fact]
    public async Task Post_WithValidPayload_PersistsEveryProperty()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var payload = NewCustomerContract("post-all");

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var persisted = await ReadCustomerByVatNumberAsync(payload.VatNumber, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(payload.Name, persisted.Name);
        Assert.Equal(payload.Address, persisted.Address);
        Assert.Equal(payload.Email, persisted.Email);
        Assert.Equal(payload.VatNumber, persisted.VatNumber);

        #endregion
    }

    [Theory]
    [InlineData(nameof(CreateCustomerContract.Name))]
    [InlineData(nameof(CreateCustomerContract.Address))]
    [InlineData(nameof(CreateCustomerContract.Email))]
    [InlineData(nameof(CreateCustomerContract.VatNumber))]
    public async Task Post_WithOneDistinctiveField_PersistsThatField(string field)
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var baseline = NewCustomerContract($"post-{field.ToLowerInvariant()}");
        var payload = WithField(baseline, field, DistinctiveValueFor(field, "post"));

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var persisted = await ReadCustomerByVatNumberAsync(payload.VatNumber, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(ValueOf(payload, field), ValueOf(persisted, field));

        #endregion
    }

    [Fact]
    public async Task Post_WithVatNumberAlreadyInUse_ReturnsConflict()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var taken = fullSample[0];
        var payload = NewCustomerContract("post-conflict") with { VatNumber = taken.VatNumber };

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Post_WithEmptyIdInPayload_IgnoresItAndCreatesCustomer()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var payload = NewCustomerContract("post-empty-id");

        var body = new CreateCustomerContract(
        Name: payload.Name,
        Address: payload.Address,
        Email: payload.Email,
        VatNumber: payload.VatNumber
        );

        #endregion

        var response = await _client.PostAsJsonAsync(baseUrl, body, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var persisted = await ReadCustomerByVatNumberAsync(payload.VatNumber, cancellationToken);

        Assert.NotNull(persisted);
        Assert.NotEqual(Guid.Empty, persisted.Id);

        #endregion
    }

    [Fact]
    public async Task Post_WithoutPayload_ReturnsBadRequest()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        using var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        #endregion

        var response = await _client.PostAsync(baseUrl, emptyBody, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        #endregion
    }

    #endregion

    #region PUT

    [Fact]
    public async Task Put_WithValidPayload_UpdatesEveryProperty()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];
        var payload = NewUpdateContract(target.Id, "put-all");

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{target.Id}", payload, cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();

        var persisted = await ReadCustomerAsync(target.Id, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(payload.Name, persisted.Name);
        Assert.Equal(payload.Address, persisted.Address);
        Assert.Equal(payload.Email, persisted.Email);
        Assert.Equal(payload.VatNumber, persisted.VatNumber);

        #endregion
    }

    [Theory]
    [InlineData(nameof(CreateCustomerContract.Name))]
    [InlineData(nameof(CreateCustomerContract.Address))]
    [InlineData(nameof(CreateCustomerContract.Email))]
    [InlineData(nameof(CreateCustomerContract.VatNumber))]
    public async Task Put_ChangingOneFieldAtATime_PersistsThatChange(string field)
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];

        // Every other field keeps its stored value: only `field` changes.
        var payload = WithField(
            new UpdateCustomerContract(
                target.Name,
                target.Address,
                target.Email,
                target.VatNumber),
            field,
            DistinctiveValueFor(field, "put"));

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{target.Id}", payload, cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();

        var persisted = await ReadCustomerAsync(target.Id, cancellationToken);

        Assert.NotNull(persisted);
        Assert.Equal(ValueOf(payload, field), ValueOf(persisted, field));

        #endregion
    }

    [Fact]
    public async Task Put_WithVatNumberAlreadyInUse_ReturnsConflict()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];
        var other = fullSample[1];

        var payload = NewUpdateContract(target.Id, "put-conflict") with { VatNumber = other.VatNumber };

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{target.Id}", payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Put_ForUnknownCustomer_ReturnsNotFound()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var unknownId = Guid.NewGuid();
        var payload = NewUpdateContract(unknownId, "put-unknown");

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{unknownId}", payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Put_WithEmptyId_ReturnsBadRequest()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var payload = NewUpdateContract(Guid.Empty, "put-empty-id");

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{Guid.Empty}", payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Put_WithoutPayload_ReturnsBadRequest()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];
        using var emptyBody = new StringContent(string.Empty, Encoding.UTF8, "application/json");

        #endregion

        var response = await _client.PutAsync($"{baseUrl}/{target.Id}", emptyBody, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Put_WithNoChanges_ReturnsConflict()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];

        // Every field identical to what is already stored.
        var payload = new UpdateCustomerContract(
            target.Name,
            target.Address,
            target.Email,
            target.VatNumber);

        #endregion

        var response = await _client.PutAsJsonAsync($"{baseUrl}/{target.Id}", payload, cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        #endregion
    }

    #endregion

    #region DELETE

    [Fact]
    public async Task Delete_ExistingCustomer_RemovesItFromReads()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        var fullSample = await SeedTwentyCustomersAsync(cancellationToken);

        var target = fullSample[0];

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{target.Id}", cancellationToken);

        #region Assert

        response.EnsureSuccessStatusCode();

        var readBack = await _client.GetAsync($"{baseUrl}/{target.Id}", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, readBack.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Delete_NonExistentCustomer_ReturnsNotFound()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        var unknownId = Guid.NewGuid();

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{unknownId}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        #endregion
    }

    [Fact]
    public async Task Delete_WithEmptyId_ReturnsBadRequest()
    {
        #region Arrange

        var cancellationToken = TestContext.Current.CancellationToken;
        await SeedTwentyCustomersAsync(cancellationToken);

        #endregion

        var response = await _client.DeleteAsync($"{baseUrl}/{Guid.Empty}", cancellationToken);

        #region Assert

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        #endregion
    }

    #endregion

    #region Helpers

    private static CreateCustomerContract NewCustomerContract(string suffix)
        => new(
            Name: $"Name {suffix}",
            Address: $"Address {suffix}",
            Email: $"{suffix}@invoyz.test",
            VatNumber: $"vat-{suffix}");

    private static UpdateCustomerContract NewUpdateContract(Guid id, string suffix)
        => new(
            Name: $"Name {suffix}",
            Address: $"Address {suffix}",
            Email: $"{suffix}@invoyz.test",
            VatNumber: $"vat-{suffix}");

    private static string DistinctiveValueFor(string field, string prefix)
        => field == nameof(CreateCustomerContract.Email)
            ? $"{prefix}-{field.ToLowerInvariant()}-distinct@invoyz.test"
            : $"{prefix}-{field.ToLowerInvariant()}-distinct";

    private static CreateCustomerContract WithField(CreateCustomerContract contract, string field, string value)
        => field switch
        {
            nameof(CreateCustomerContract.Name) => contract with { Name = value },
            nameof(CreateCustomerContract.Address) => contract with { Address = value },
            nameof(CreateCustomerContract.Email) => contract with { Email = value },
            nameof(CreateCustomerContract.VatNumber) => contract with { VatNumber = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

    private static UpdateCustomerContract WithField(UpdateCustomerContract contract, string field, string value)
        => field switch
        {
            nameof(CreateCustomerContract.Name) => contract with { Name = value },
            nameof(CreateCustomerContract.Address) => contract with { Address = value },
            nameof(CreateCustomerContract.Email) => contract with { Email = value },
            nameof(CreateCustomerContract.VatNumber) => contract with { VatNumber = value },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

    private static string ValueOf(UpdateCustomerContract contract, string field)
        => field switch
        {
            nameof(UpdateCustomerContract.Name) => contract.Name,
            nameof(UpdateCustomerContract.Address) => contract.Address,
            nameof(UpdateCustomerContract.Email) => contract.Email,
            nameof(UpdateCustomerContract.VatNumber) => contract.VatNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

    private static string ValueOf(CreateCustomerContract contract, string field)
    => field switch
    {
        nameof(CreateCustomerContract.Name) => contract.Name,
        nameof(CreateCustomerContract.Address) => contract.Address,
        nameof(CreateCustomerContract.Email) => contract.Email,
        nameof(CreateCustomerContract.VatNumber) => contract.VatNumber,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    private static string ValueOf(CustomerEntity entity, string field)
        => field switch
        {
            nameof(CreateCustomerContract.Name) => entity.Name,
            nameof(CreateCustomerContract.Address) => entity.Address,
            nameof(CreateCustomerContract.Email) => entity.Email,
            nameof(CreateCustomerContract.VatNumber) => entity.VatNumber,
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };

    private async Task<CustomerEntity?> ReadCustomerAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    private async Task<CustomerEntity?> ReadCustomerByVatNumberAsync(string vatNumber, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await appDbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.VatNumber == vatNumber, cancellationToken);
    }

    #endregion
}
