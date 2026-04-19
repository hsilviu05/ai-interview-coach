using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/admin/observability")]
    [Authorize(Policy = AuthorizationPolicies.AdminObservabilityAccess)]
    public sealed class AdminObservabilityController : ControllerBase
    {
        private readonly IObservabilityService _observabilityService;

        public AdminObservabilityController(IObservabilityService observabilityService)
        {
            _observabilityService = observabilityService;
        }

        [HttpGet]
        public IActionResult GetSnapshot()
        {
            return Ok(_observabilityService.GetSnapshot());
        }
    }
}
