using System.Collections.Generic;

public class GetAllCustomersResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public CustomersData CustomersData { get; set; }
}

public class CustomersData
{
    public List<CustomerDto> Customers { get; set; }
    public int TotalCount { get; set; }
}
