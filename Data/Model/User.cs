using System.ComponentModel.DataAnnotations.Schema;

namespace iThome2024.SalesService.Data.Model;

public class User
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Column(TypeName = "varchar(200)")]
    public string? Username { get; set; }
    [Column(TypeName = "varchar(200)")]
    public string? Password { get; set; }
    [Column(TypeName = "timestamp")]
    public DateTime CreateTime { get; set; }
}
