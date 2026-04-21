using System.Security.Claims;
using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.API.RateLimiting;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/problems")]
    [Authorize]
    public class ProblemsController : ControllerBase
    {
        private readonly IProblemService _problemService;
        private readonly IProblemTemplateService _problemTemplateService;
        private readonly IPracticeProblemHintRequestService _practiceProblemHintRequestService;

        public ProblemsController(
            IProblemService problemService,
            IProblemTemplateService problemTemplateService,
            IPracticeProblemHintRequestService practiceProblemHintRequestService)
        {
            _problemService = problemService;
            _problemTemplateService = problemTemplateService;
            _practiceProblemHintRequestService = practiceProblemHintRequestService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProblemSummaryResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllProblems()
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();
            var problems = await _problemService.GetAllProblemsAsync(currentUserId, currentUserRole);
            return Ok(problems);
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ProblemResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProblemById(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var currentUserRole = GetCurrentUserRole();
            var problem = await _problemService.GetProblemByIdAsync(id, currentUserId, currentUserRole);
            if (problem == null)
                return NotFound(new { message = "Problem not found." });

            return Ok(problem);
        }

        [HttpGet("templates")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [ProducesResponseType(typeof(IReadOnlyList<ProblemTemplateResponseDto>), StatusCodes.Status200OK)]
        public IActionResult GetProblemTemplates()
        {
            var templates = _problemTemplateService.GetTemplates();
            return Ok(templates);
        }

        [HttpPost("signature-preview")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [ProducesResponseType(typeof(ProblemSignaturePreviewResponseDto), StatusCodes.Status200OK)]
        public IActionResult GetSignaturePreview([FromBody] ProblemSignatureDefinitionDto signature)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var preview = _problemTemplateService.GetSignaturePreview(signature);
            return Ok(preview);
        }

        [HttpPost("{id:guid}/hints")]
        [Authorize(Policy = AuthorizationPolicies.CandidateWorkspaceAccess)]
        [EnableRateLimiting(RateLimitingPolicies.CandidateSubmissionFlow)]
        [ProducesResponseType(typeof(ProblemHintResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GeneratePracticeHint(
            Guid id,
            [FromBody] ProblemHintRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            try
            {
                var hint = await _practiceProblemHintRequestService.GeneratePracticeHintAsync(
                    id,
                    GetCurrentUserId(),
                    GetCurrentUserRole(),
                    request,
                    cancellationToken);

                return Ok(hint);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Problem not found." });
            }
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [EnableRateLimiting(RateLimitingPolicies.AdminMutation)]
        [ProducesResponseType(typeof(ProblemResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateProblem([FromBody] CreateProblemRequestDto createRequest)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var currentUserId = GetCurrentUserId();
            var createdProblem = await _problemService.CreateProblemAsync(currentUserId, createRequest);

            return CreatedAtAction(
                nameof(GetProblemById),
                new { id = createdProblem.Id },
                createdProblem);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [EnableRateLimiting(RateLimitingPolicies.AdminMutation)]
        [ProducesResponseType(typeof(ProblemResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProblem(Guid id, [FromBody] UpdateProblemRequestDto updateRequest)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var currentUserId = GetCurrentUserId();
            var updatedProblem = await _problemService.UpdateProblemAsync(id, currentUserId, updateRequest);

            if (updatedProblem == null)
                return NotFound(new { message = "Problem not found." });

            return Ok(updatedProblem);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [EnableRateLimiting(RateLimitingPolicies.AdminMutation)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteProblem(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var deleted = await _problemService.DeleteProblemAsync(id, currentUserId, IsAdmin());

            if (!deleted)
                return NotFound(new { message = "Problem not found." });

            return NoContent();
        }

        [HttpPost("catalog/replace-with-starter-set")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [EnableRateLimiting(RateLimitingPolicies.AdminMutation)]
        [ProducesResponseType(typeof(ReplaceProblemCatalogResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> ReplaceCatalogWithStarterSet()
        {
            var currentUserId = GetCurrentUserId();
            var result = await _problemService.ReplaceCatalogWithStarterSetAsync(currentUserId);
            return Ok(result);
        }

        [HttpPost("{problemId:guid}/testcases")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [EnableRateLimiting(RateLimitingPolicies.AdminMutation)]
        [ProducesResponseType(typeof(TestCaseResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddTestCase(Guid problemId, [FromBody] CreateTestCaseRequestDto createTestCaseRequest)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var currentUserId = GetCurrentUserId();
            var testCase = await _problemService.AddTestCaseAsync(
                problemId,
                currentUserId,
                IsAdmin(),
                createTestCaseRequest);
            return Ok(testCase);
        }

        [HttpGet("{problemId:guid}/testcases")]
        [Authorize(Policy = AuthorizationPolicies.AdminProblemManagement)]
        [ProducesResponseType(typeof(IEnumerable<TestCaseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTestCases(Guid problemId, [FromQuery] bool includeHidden = false)
        {
            var currentUserId = GetCurrentUserId();
            var testCases = await _problemService.GetTestCasesAsync(
                problemId,
                currentUserId,
                IsAdmin(),
                includeHidden);
            return Ok(testCases);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst(ClaimTypes.Name)
                              ?? User.FindFirst("sub");

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("Invalid user token.");

            return userId;
        }

        private UserRole GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)
                            ?? User.FindFirst("role");

            if (roleClaim is null || !Enum.TryParse<UserRole>(roleClaim.Value, true, out var role))
                throw new UnauthorizedAccessException("Invalid user role.");

            return role;
        }

        private bool IsAdmin() => GetCurrentUserRole() == UserRole.Admin;
    }
}
