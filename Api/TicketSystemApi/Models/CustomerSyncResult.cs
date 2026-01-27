using System.Collections.Generic;

namespace TicketSystemApi.Models
{
    public class CustomerSyncResult
    {
        public int TotalProcessed { get; set; }
        public int AccountsCreated { get; set; }
        public int AccountsUpdated { get; set; }
        public int AccountsSkipped { get; set; }
        public int TotalAccountsInTaadeen { get; set; }

        public List<CustomerDto> TaadeenAccounts { get; set; } = new List<CustomerDto>();
        public List<CustomerDto> CreatedAccounts { get; set; } = new List<CustomerDto>();
        public List<CustomerDto> UpdatedAccounts { get; set; } = new List<CustomerDto>();
    }
}
