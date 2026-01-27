using System.Threading.Tasks;
using System.Web.Http;
using TicketSystemApi.Services;

namespace TicketSystemApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/customers")]
    public class CustomerSyncController : ApiController
    {
        [HttpPost]
        [Route("sync")]
        public async Task<IHttpActionResult> SyncCustomers()
        {
            var syncService = new CustomerSyncService();
            int processed = await syncService.Sync();

            return Ok(new
            {
                Status = "Success",
                TotalProcessed = processed.ToString(),

                // 🔹 ACCOUNTS
                TotalAccountsInTaadeen = syncService.TaadeenAccounts.Count.ToString(),
                AccountsCreated = syncService.CreatedAccounts.Count.ToString(),
                AccountsUpdated = syncService.UpdatedAccounts.Count.ToString(),
                AccountsSkipped = (
                    syncService.TaadeenAccounts.Count
                    - syncService.CreatedAccounts.Count
                    - syncService.UpdatedAccounts.Count
                ).ToString(),

                // 🔹 CONTACTS
                TotalIndividualsInTaadeen = syncService.TaadeenIndividuals.Count.ToString(),
                ContactsCreated = syncService.CreatedContacts.Count.ToString(),
                ContactsUpdated = syncService.UpdatedContacts.Count.ToString(),
                ContactsSkipped = (
                    syncService.TaadeenIndividuals.Count
                    - syncService.CreatedContacts.Count
                    - syncService.UpdatedContacts.Count
                ).ToString(),

                // 🔹 OPTIONAL DETAILS (debug / audit)
                AccountsFromTaadeen = syncService.TaadeenAccounts,
                CreatedAccounts = syncService.CreatedAccounts,
                UpdatedAccounts = syncService.UpdatedAccounts,

                IndividualsFromTaadeen = syncService.TaadeenIndividuals,
                CreatedContacts = syncService.CreatedContacts,
                UpdatedContacts = syncService.UpdatedContacts
            });
        }
    }
}
