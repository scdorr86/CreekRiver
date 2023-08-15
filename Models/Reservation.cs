using System.Runtime.CompilerServices;

namespace CreekRiver.Models;

public class Reservation
{
    public int Id { get; set; }
    public int CampsiteId { get; set; }
    public Campsite Campsite { get; set; }
    public int UserProfileId { get; set; }
    public UserProfile UserProfile { get; set; }
    public DateTime CheckinDate { get; set; }
    public DateTime CheckoutDate { get; set; }
    public int TotalNights => (CheckoutDate - CheckinDate).Days;

    private static readonly decimal _reservationBaseFee = 10M;
    public decimal? TotalCost
    {
        get
        {
            if (Campsite?.CampsiteType != null)
            {
                return Campsite.CampsiteType.FeePerNight * TotalNights + _reservationBaseFee;
            }
            return null;
        }
    }
}