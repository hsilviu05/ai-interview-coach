using System.Security.Claims;
using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.API.RateLimiting;
using AIInterviewCoach.Application.DTOs.Interviews;
using AIInterviewCoach.Application.Interfaces.Services;
using AIInterviewCoach.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/interviews")]
    [Authorize]
    public class InterviewsController : ControllerBase
    {
        private readonly IInterviewService _interviewService;

        public InterviewsController(IInterviewService interviewService)
        {
            _interviewService = interviewService;
        }

        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(typeof(InterviewResponseDto), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateInterview([FromBody] CreateInterviewRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.CreateInterviewAsync(interviewerId, request);

            return CreatedAtAction(nameof(GetInterviewById), new { id = result.Id }, result);
        }

        [HttpPost("{id:guid}/problems")]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddProblemToInterview(Guid id, [FromBody] AddProblemToInterviewRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var interviewerId = GetCurrentUserId();
            var added = await _interviewService.AddProblemAsync(id, request, interviewerId);

            if (!added)
                return NotFound(new { message = "Interview not found." });

            return Ok(new { message = "Problem added to interview successfully." });
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(typeof(InterviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInterviewById(Guid id)
        {
            var interviewerId = GetCurrentUserId();
            var interview = await _interviewService.GetByIdAsync(id, interviewerId, User.IsInRole(UserRole.Admin.ToString()));

            if (interview is null)
                return NotFound(new { message = "Interview not found." });

            return Ok(interview);
        }

        [HttpGet("token/{token}")]
        [EnableRateLimiting(RateLimitingPolicies.PublicInterviewTokenAccess)]
        [ProducesResponseType(typeof(InterviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInterviewByToken(string token)
        {
            var interview = await _interviewService.GetByTokenAsync(token);

            if (interview is null)
                return NotFound(new { message = "Interview not found." });

            return Ok(interview);
        }

        [HttpPost("token/{token}/start")]
        [Authorize(Policy = AuthorizationPolicies.CandidateWorkspaceAccess)]
        [EnableRateLimiting(RateLimitingPolicies.PublicInterviewTokenAccess)]
        [ProducesResponseType(typeof(InterviewSessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartInterviewSession(string token)
        {
            var candidateId = GetCurrentUserId();
            var session = await _interviewService.StartSessionAsync(token, candidateId);

            return Ok(session);
        }

        [HttpPost("sessions/{sessionId:guid}/complete")]
        [Authorize(Policy = AuthorizationPolicies.CandidateWorkspaceAccess)]
        [ProducesResponseType(typeof(InterviewSessionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CompleteInterviewSession(Guid sessionId)
        {
            var candidateId = GetCurrentUserId();
            var result = await _interviewService.CompleteSessionAsync(sessionId, candidateId);

            return Ok(result);
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("Invalid user token.");

            return userId;
        }

        [HttpGet("{interviewId:guid}/sessions")]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(typeof(IEnumerable<InterviewSessionResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInterviewSessions(Guid interviewId)
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetInterviewSessionsAsync(interviewId, interviewerId);

            return Ok(result);
        }

        [HttpGet("sessions/{sessionId:guid}")]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(typeof(InterviewSessionDetailsDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetInterviewSessionDetails(Guid sessionId)
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetInterviewSessionDetailsAsync(sessionId, interviewerId);

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.InterviewerWorkspaceAccess)]
        [ProducesResponseType(typeof(IEnumerable<InterviewResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyInterviews()
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetMineAsync(interviewerId);

            return Ok(result);
        }
    }
}
