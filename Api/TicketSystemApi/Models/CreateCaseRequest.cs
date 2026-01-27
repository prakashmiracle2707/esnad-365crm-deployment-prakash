using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TicketSystemApi.Models
{
    public class CreateCaseRequest
    {
        public string Title { get; set; }

        // REQUIRED by CRM
        public Guid CustomerId { get; set; }

        // Optional (default = account)
        // Allowed values: "account" or "contact"
        public string CustomerType { get; set; }
    }
}
