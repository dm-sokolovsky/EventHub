using EventHub.Api.Models;
using ValidationException = EventHub.Api.Common.Exceptions.ValidationException;

namespace EventHub.Tests;

public sealed class EventTests
{
    private static Event CreateEvent(int totalSeats) => Event.Create(
        "Event",
        DateTime.UtcNow.AddDays(1),
        DateTime.UtcNow.AddDays(1).AddHours(2),
        totalSeats);

    [Fact]
    public void Update_ReducingTotalSeatsBelowBookedSeats_ThrowsValidationException()
    {
        // Arrange
        var @event = CreateEvent(totalSeats: 10);
        @event.TryReserveSeats(7); // AvailableSeats = 3, забронировано 7

        // Act
        var act = () => @event.Update(@event.Title, @event.StartAt, @event.EndAt, @event.Description, totalSeats: 5);

        // Assert
        var ex = Assert.Throws<ValidationException>(act);
        Assert.Contains(nameof(Event.TotalSeats), ex.Errors.Keys);
    }

    [Fact]
    public void Update_ReducingTotalSeatsToExactlyBookedSeats_SetsAvailableSeatsToZero()
    {
        // Arrange
        var @event = CreateEvent(totalSeats: 10);
        @event.TryReserveSeats(7); // AvailableSeats = 3, забронировано 7

        // Act
        @event.Update(@event.Title, @event.StartAt, @event.EndAt, @event.Description, totalSeats: 7);

        // Assert
        Assert.Equal(7, @event.TotalSeats);
        Assert.Equal(0, @event.AvailableSeats);
    }

    [Fact]
    public void Update_IncreasingTotalSeats_IncreasesAvailableSeatsByTheSameAmount()
    {
        // Arrange
        var @event = CreateEvent(totalSeats: 10);
        @event.TryReserveSeats(7); // AvailableSeats = 3, забронировано 7

        // Act
        @event.Update(@event.Title, @event.StartAt, @event.EndAt, @event.Description, totalSeats: 20);

        // Assert
        Assert.Equal(20, @event.TotalSeats);
        Assert.Equal(13, @event.AvailableSeats); // 20 - 7 забронированных
    }
}
