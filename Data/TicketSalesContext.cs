using System.ComponentModel.DataAnnotations.Schema;
using iThome2024.SalesService.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace iThome2024.SalesService.Data;

public class TicketSalesContext : DbContext
{
    public DbSet<User> User { get; set; }
    public DbSet<Event> Event { get; set; }
    public DbSet<Seat> Seat { get; set; }


    public TicketSalesContext(DbContextOptions<TicketSalesContext> options) : base(options)
    {
    }
}