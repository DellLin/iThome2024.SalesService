using System.ComponentModel.DataAnnotations.Schema;

namespace iThome2024.SalesService.Data.Model;

public class Ticket
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int SeatId { get; set; }
    public int UserId { get; set; }
    [Column(TypeName = "timestamp")]
    public DateTime CreateTime { get; set; }
    public Seat Seat { get; set; }
    public User User { get; set; }
}
