using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Security.Claims;
using System.Web.Http;
using TicketSystemApi.Models;
using TicketSystemApi.Services;

namespace TicketSystemApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/cases")]
    public class CreateCaseController : ApiController
    {
        [Authorize]
        [HttpPost]
        [Route("create")]
        public IHttpActionResult CreateCase(CreateCaseRequest request)
        {
            try
            {
                var identity = (ClaimsIdentity)User.Identity;

                var username = identity.FindFirst("crm_username")?.Value;
                var password = identity.FindFirst("crm_password")?.Value;

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return Unauthorized();

                if (request == null)
                    return BadRequest("Request body is missing.");

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest("Title is required.");

                if (request.CustomerId == Guid.Empty)
                    return BadRequest("CustomerId is required.");

                var service = new CrmService().GetService1(username, password);

                var incident = new Entity("incident");

                // 🔹 Required fields
                incident["title"] = request.Title;

                // 🔹 Customer (MANDATORY)
                var customerType = request.CustomerType?.ToLower() == "contact"
                    ? "contact"
                    : "account";

                incident["customerid"] = new EntityReference(customerType, request.CustomerId);

                // 🔹 Create Case
                var caseId = service.Create(incident);

                var createdCase = service.Retrieve(
                    "incident",
                    caseId,
                    new ColumnSet("ticketnumber", "createdon", "statuscode")
                );

                return Ok(new
                {
                    CaseId = caseId,
                    TicketNumber = createdCase.GetAttributeValue<string>("ticketnumber"),
                    CreatedOn = createdCase.GetAttributeValue<DateTime?>("createdon"),
                    Status = createdCase.FormattedValues.Contains("statuscode")
                                ? createdCase.FormattedValues["statuscode"]
                                : null,
                    Message = "Case created successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception($"Create Case failed: {ex.Message}"));
            }
        }
    }
}
