public class UpsertResult
{
    public bool AccountCreated { get; set; }
    public bool AccountUpdated { get; set; }
    public bool AccountSkipped { get; set; }

    public bool ContactCreated { get; set; }
    public bool ContactUpdated { get; set; }
    public bool ContactSkipped { get; set; }
}
