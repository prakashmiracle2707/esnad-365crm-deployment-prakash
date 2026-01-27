using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TicketSystemApi.Models;

namespace TicketSystemApi.Services
{
    public class CustomerSyncService
    {
        private readonly IOrganizationService _service;

        public List<CustomerDto> TaadeenAccounts = new List<CustomerDto>();
        public List<CustomerDto> CreatedAccounts = new List<CustomerDto>();
        public List<CustomerDto> UpdatedAccounts = new List<CustomerDto>();

        public List<CustomerDto> TaadeenIndividuals = new List<CustomerDto>();
        public List<CustomerDto> CreatedContacts = new List<CustomerDto>();
        public List<CustomerDto> UpdatedContacts = new List<CustomerDto>();

        public CustomerSyncService()
        {
            _service = new CrmService().GetService();
        }

        public async Task<int> Sync()
        {
            int processed = 0;
            int startIndex = 0;
            int maxRecords = 100;

            while (true)
            {
                var response = await CallTaadeenApi(startIndex, maxRecords);
                var customers = response.CustomersData.Customers;

                if (customers == null || customers.Count == 0)
                    break;

                foreach (var customer in customers)
                {
                    processed++;

                    if (customer.CompanyId > 0 && customer.CustomerTypeID == 1)
                    {
                        TaadeenAccounts.Add(customer);

                        var result = UpsertAccount(customer);
                        if (result.AccountCreated)
                            CreatedAccounts.Add(customer);
                        else if (result.AccountUpdated)
                            UpdatedAccounts.Add(customer);
                    }
                    else
                    {
                        TaadeenIndividuals.Add(customer);

                        var result = UpsertContact(customer);
                        if (result.ContactCreated)
                            CreatedContacts.Add(customer);
                        else if (result.ContactUpdated)
                            UpdatedContacts.Add(customer);
                    }
                }

                startIndex += maxRecords;
                if (startIndex >= response.CustomersData.TotalCount)
                    break;
            }

            return processed;
        }

        // =======================
        // 🔁 ACCOUNT UPSERT
        // =======================
        private UpsertResult UpsertAccount(CustomerDto dto)
        {
            var result = new UpsertResult();

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet(
                    "accountid",
                    "name",
                    "new_crnumber",
                    "new_unifiednumber",
                    "new_customertypeid",
                    "new_externalcompanyid" // 👈 REQUIRED
                ),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "new_externalcompanyid",
                            ConditionOperator.Equal,
                            dto.CompanyId.ToString()
                        )
                    }
                }
            };

            var existing = _service.RetrieveMultiple(query).Entities.FirstOrDefault();
            var account = existing ?? new Entity("account");

            bool hasChanges = false;
            hasChanges |= SetIfChanged(account, "name", dto.CompanyName);
            hasChanges |= SetIfChanged(account, "new_crnumber", dto.CRnumber);
            hasChanges |= SetIfChanged(account, "new_unifiednumber", dto.UnifiedNumber);
            hasChanges |= SetIfChanged(account, "new_customertypeid", dto.CustomerTypeID.ToString());
            hasChanges |= SetIfChanged(account, "new_externalcompanyid", dto.CompanyId.ToString());

            if (existing == null)
            {
                account.Id = _service.Create(account);
                result.AccountCreated = true;
            }
            else if (hasChanges)
            {
                _service.Update(account);
                result.AccountUpdated = true;
            }
            else
            {
                result.AccountSkipped = true;
            }

            return result;
        }

        // =======================
        // 👤 CONTACT UPSERT
        // =======================
        private UpsertResult UpsertContact(CustomerDto dto)
        {
            var result = new UpsertResult();
            string externalProfileId = dto.ProfileID.ToString();

            var query = new QueryExpression("contact")
            {
            ColumnSet = new ColumnSet(
                "contactid",
                "firstname",
                "lastname",
                "emailaddress1",
                "telephone1",
                "parentcustomerid",
                "new_externalprofileid" // 👈 REQUIRED
            ),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "new_externalprofileid",
                            ConditionOperator.Equal,
                            externalProfileId
                        )
                    }
                }
            };

            var existing = _service.RetrieveMultiple(query).Entities.FirstOrDefault();
            var contact = existing ?? new Entity("contact");

            bool hasChanges = false;
            hasChanges |= SetIfChanged(
                contact,
                "firstname",
                string.IsNullOrWhiteSpace(dto.InvestorFirstNameEN)
                    ? "Investor"
                    : dto.InvestorFirstNameEN
            );

            hasChanges |= SetIfChanged(
                contact,
                "lastname",
                string.IsNullOrWhiteSpace(dto.InvestorLastNameEN)
                    ? dto.CompanyName
                    : dto.InvestorLastNameEN
            );

            hasChanges |= SetIfChanged(contact, "emailaddress1", dto.InvestorEmail);
            hasChanges |= SetIfChanged(contact, "telephone1", dto.InvestorPhoneNumber);
            hasChanges |= SetIfChanged(contact, "new_externalprofileid", externalProfileId);

            if (dto.CompanyId > 0)
            {
                var accountRef = GetAccountReference(dto.CompanyId);
                if (accountRef != null)
                {
                    hasChanges |= SetIfChanged(contact, "parentcustomerid", accountRef);
                }
            }

            if (existing == null)
            {
                contact.Id = _service.Create(contact);
                result.ContactCreated = true;
            }
            else if (hasChanges)
            {
                _service.Update(contact);
                result.ContactUpdated = true;
            }
            else
            {
                result.ContactSkipped = true;
            }

            return result;

        }

        // =======================
        // 🔎 ACCOUNT LOOKUP
        // =======================
        private EntityReference GetAccountReference(int companyId)
        {
            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("accountid"),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(
                            "new_externalcompanyid",
                            ConditionOperator.Equal,
                            companyId.ToString()
                        )
                    }
                }
            };

            var account = _service.RetrieveMultiple(query).Entities.FirstOrDefault();
            return account?.ToEntityReference();
        }

        // =======================
        // 🧠 CHANGE DETECTOR
        // =======================
        private bool SetIfChanged(Entity entity, string attributeName, object newValue)
        {
            // Normalize null / empty string
            if (newValue is string s && string.IsNullOrWhiteSpace(s))
                newValue = null;

            if (!entity.Attributes.Contains(attributeName))
            {
                if (newValue == null)
                    return false;

                entity[attributeName] = newValue;
                return true;
            }

            var existingValue = entity[attributeName];

            // 🔹 String comparison
            if (existingValue is string existingString && newValue is string newString)
            {
                if (string.Equals(
                        existingString?.Trim(),
                        newString?.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            // 🔹 EntityReference comparison
            else if (existingValue is EntityReference er1 && newValue is EntityReference er2)
            {
                if (er1.Id == er2.Id && er1.LogicalName == er2.LogicalName)
                    return false;
            }
            // 🔹 Generic comparison
            else if (Equals(existingValue, newValue))
            {
                return false;
            }

            entity[attributeName] = newValue;
            return true;
        }

        // =======================
        // 🌐 API CALL
        // =======================
        private async Task<GetAllCustomersResponse> CallTaadeenApi(int startIndex, int maxRecords)
        {
            string apiUrl =
                "https://platform-test.taadeen.dev/CRM_Integration_APi/rest/v1/GetAllCustomers";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add(
                    "X-Contacts-AppId",
                    "ghjfxdfAvs596vcGfsvf0ef1"
                );
                client.DefaultRequestHeaders.Add(
                    "X-Contacts-Key",
                    "6tsdgdjl9fsKDd5zsvnwmdjosDmrufbs93susadLHDvjfhbnwtTRbsnucnrb"
                );

                var body = new
                {
                    StartIndex = startIndex.ToString(),
                    MaxRecords = maxRecords.ToString()
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<GetAllCustomersResponse>(json);
            }
        }
    }
}
