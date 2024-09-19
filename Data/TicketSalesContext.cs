using Microsoft.EntityFrameworkCore;

namespace iThome2024.SalesService.Data;

public class TicketSalesContext : DbContext
{
    public TicketSalesContext(DbContextOptions<TicketSalesContext> options) : base(options)
    {
    }
}
