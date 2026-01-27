using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TicketSystemApi.Models;

namespace TicketSystemApi.Services
{
    public class TaadeenApiService
    {
        public async Task<GetAllCustomersResponse> GetCustomers(int startIndex, int maxRecords)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Contacts-AppId", "ghjfxdfAvs596vcGfsvf0ef1");
                client.DefaultRequestHeaders.Add("X-Contacts-Key", "6tsdgdjl9fsKDd5zsvnwmdjosDmrufbs93susadLHDvjfhbnwtTRbsnucnrb");

                var body = new
                {
                    StartIndex = startIndex,
                    MaxRecords = maxRecords
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    "https://platform-test.taadeen.dev/CRM_Integration_APi/rest/v1/GetAllCustomers",
                    content);

                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<GetAllCustomersResponse>(
                    await response.Content.ReadAsStringAsync());
            }
        }
    }
}
