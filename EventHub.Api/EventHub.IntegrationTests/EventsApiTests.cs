using System.Net;
using System.Net.Http.Json;
using EventHub.Api.Contracts;
using EventHub.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EventHub.IntegrationTests;

public class EventsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public EventsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostEvent_ReturnsCreated_WithLocationHeaderAndBody()
    {
        var payload = new
        {
            title = $"http_create_{Guid.NewGuid()}",
            description = "desc",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1),
            totalSeats = 10
        };

        var response = await _client.PostAsJsonAsync("/api/events", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<ApiResult<EventInfoDto>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.Equal(payload.title, body.Data.Title);
        Assert.EndsWith($"/api/events/{body.Data.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task PostEvent_MissingTitle_ReturnsBadRequest()
    {
        var payload = new
        {
            description = "desc",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1)
        };

        var response = await _client.PostAsJsonAsync("/api/events", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAllEvents_InvalidPage_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/api/events?page=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiBaseResult>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task GetAllEvents_FilterByTitle_ReturnsOnlyMatchingEvent()
    {
        var uniqueTitle = $"http_filter_{Guid.NewGuid()}";
        var payload = new
        {
            title = uniqueTitle,
            description = "desc",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1),
            totalSeats = 10
        };
        await _client.PostAsJsonAsync("/api/events", payload);

        var response = await _client.GetAsync($"/api/events?title={Uri.EscapeDataString(uniqueTitle)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiResult<PaginatedResult<EventInfoDto>>>();
        Assert.NotNull(body);
        Assert.Equal(1, body!.Data.TotalCount);
        Assert.Equal(uniqueTitle, Assert.Single(body.Data.Items).Title);
    }

    [Fact]
    public async Task DeleteEvent_NotFound_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/events/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ApiBaseResult>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
    }

    [Fact]
    public async Task PutEvent_NotFound_ReturnsNotFound()
    {
        var payload = new
        {
            title = "does_not_matter",
            description = "desc",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1),
            totalSeats = 10
        };

        var response = await _client.PutAsJsonAsync($"/api/events/{Guid.NewGuid()}", payload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PutEvent_TotalSeatsBelowBookedSeats_ReturnsBadRequest()
    {
        var payload = new
        {
            title = $"http_update_{Guid.NewGuid()}",
            description = "desc",
            startAt = DateTime.UtcNow,
            endAt = DateTime.UtcNow.AddHours(1),
            totalSeats = 10
        };

        var createResponse = await _client.PostAsJsonAsync("/api/events", payload);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdBody = await createResponse.Content.ReadFromJsonAsync<ApiResult<EventInfoDto>>();
        Assert.NotNull(createdBody);
        var eventId = createdBody!.Data.Id;

        // Бронируем 6 мест из 10, чтобы TotalSeats - AvailableSeats стало 6
        for (var i = 0; i < 6; i++)
        {
            var bookResponse = await _client.PostAsync($"/api/events/{eventId}/book", null);
            Assert.Equal(HttpStatusCode.Accepted, bookResponse.StatusCode);
        }

        var updatePayload = new
        {
            title = payload.title,
            description = payload.description,
            startAt = payload.startAt,
            endAt = payload.endAt,
            totalSeats = 3 // меньше уже забронированных 6 мест
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/events/{eventId}", updatePayload);

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var updateBody = await updateResponse.Content.ReadFromJsonAsync<ApiBaseResult>();
        Assert.NotNull(updateBody);
        Assert.False(updateBody!.Success);
    }
}
