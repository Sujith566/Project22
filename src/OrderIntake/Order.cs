namespace OrderIntake;

public class Order
{
    public string OrderId { get; set; } = "";
    public string PatientId { get; set; } = "";
    public string SpecimenId { get; set; } = "";
    public string SpecimenType { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime CollectionDate { get; set; }
    public List<string> RequestedTests { get; set; } = new();
}