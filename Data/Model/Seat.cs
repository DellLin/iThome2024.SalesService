using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace iThome2024.SalesService.Data.Model;

public class Seat
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int EventId { get; set; }
    [Column(TypeName = "varchar(50)")]
    public string? Area { get; set; }
    [Column(TypeName = "varchar(200)")]
    public required string Name { get; set; }
    public int Status { get; set; }
    [JsonIgnore]
    public Event Event { get; set; }
}
