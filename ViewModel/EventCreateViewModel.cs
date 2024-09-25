namespace iThome2024.SalesService.ViewModel;

public class EventCreateViewModel
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime StartSalesDate { get; set; }
    public DateTime EndSalesDate { get; set; }
    public string? Description { get; set; }
    public string? Remark { get; set; }
    public List<SeatViewModel>? Seats { get; set; }

}
