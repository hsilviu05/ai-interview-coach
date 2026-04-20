using System.Security.Claims;
using AIInterviewCoach.API.Authorization;
using AIInterviewCoach.API.RateLimiting;
using AIInterviewCoach.Application.DTOs.Submissions;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    [Authorize(Policy = AuthorizationPolicies.CandidateWorkspaceAccess)]
    [EnableRateLimiting(RateLimitingPolicies.CandidateSubmissionFlow)]
    public class SubmissionsController : ControllerBase
    {
        private readonly ISubmissionService _submissionService;

        public SubmissionsController(ISubmissionService submissionService)
        {
            _submissionService = submissionService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(SubmissionResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreateSubmission([FromBody] CreateSubmissionRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var candidateId = GetCurrentUserId();
            var result = await _submissionService.CreateSubmissionAsync(candidateId, request);

            return Ok(result);
        }

        [HttpGet("me")]
        [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMySubmissions()
        {
            var candidateId = GetCurrentUserId();
            var result = await _submissionService.GetMySubmissionsAsync(candidateId);

            return Ok(result);
        }

        [HttpGet("session/{interviewSessionId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByInterviewSession(Guid interviewSessionId)
        {
            var candidateId = GetCurrentUserId();
            var result = await _submissionService.GetByInterviewSessionAsync(interviewSessionId, candidateId);

            return Ok(result);
        }

        [HttpDelete("problem/{problemId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ResetProblem(Guid problemId, [FromQuery] Guid? interviewSessionId)
        {
            var candidateId = GetCurrentUserId();
            await _submissionService.ResetProblemAsync(candidateId, problemId, interviewSessionId);

            return NoContent();
        }

        [HttpDelete("session/{interviewSessionId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ResetInterviewSession(Guid interviewSessionId)
        {
            var candidateId = GetCurrentUserId();
            await _submissionService.ResetInterviewSessionAsync(candidateId, interviewSessionId);

            return NoContent();
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub");

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                throw new UnauthorizedAccessException("Invalid user token.");

            return userId;
        }
    }
}
