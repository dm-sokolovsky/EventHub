using EventHab.Application.Repositories;
using EventHab.Application.Services.Abstractions;
using EventHub.Domain.Exceptions;

namespace EventHab.Application;

public sealed class CancelBookingHandler(IBookingRepository bookingRepository, ICurrentUser user)
{
    private readonly IBookingRepository _bookingRepository = bookingRepository;
    private readonly ICurrentUser _currentUser = user;

    public async Task HandleAsync(Guid bookingId, CancellationToken ct)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, ct)
            ?? throw new NotFoundException("Booking not found");
        
        if (booking.UserId != _currentUser.Id && !user.IsAdmin)
            throw new BookingAccessDeniedException(bookingId, _currentUser.Id);
        
        booking.Cancel();

        await _bookingRepository.SaveChangesAsync(ct);

    }
}