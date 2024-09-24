using System.ComponentModel.DataAnnotations.Schema;

namespace iThome2024.SalesService.Data.Model;

public class Event
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Column(TypeName = "varchar(200)")]
    public required string Name { get; set; }
    [Column(TypeName = "timestamp")]
    public DateTime EventDate { get; set; }
    [Column(TypeName = "timestamp")]
    public DateTime StartSalesDate { get; set; }
    [Column(TypeName = "timestamp")]
    public DateTime EndSalesDate { get; set; }
    [Column(TypeName = "varchar(2000)")]
    public string? Description { get; set; }
    [Column(TypeName = "varchar(500)")]
    public string? Remark { get; set; }
    public List<Seat> Seats { get; } = new();

}