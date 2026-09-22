namespace OrderIntake;

public class OrderResult
{
    public string Status { get; set; } = "";
    public Order? Order { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
}