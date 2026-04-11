using System.Security.Claims;
using AIInterviewCoach.Application.DTOs.Interviews;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> CreateInterview([FromBody] CreateInterviewRequestDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.CreateInterviewAsync(interviewerId, request);

            return CreatedAtAction(nameof(GetInterviewById), new { id = result.Id }, result);
        }

        [HttpPost("{id:guid}/problems")]
        [Authorize(Roles = "Interviewer,Admin")]
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
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> GetInterviewById(Guid id)
        {
            var interviewerId = GetCurrentUserId();
            var interview = await _interviewService.GetByIdAsync(id, interviewerId, User.IsInRole("Admin"));

            if (interview is null)
                return NotFound(new { message = "Interview not found." });

            return Ok(interview);
        }

        [HttpGet("token/{token}")]
        public async Task<IActionResult> GetInterviewByToken(string token)
        {
            var interview = await _interviewService.GetByTokenAsync(token);

            if (interview is null)
                return NotFound(new { message = "Interview not found." });

            return Ok(interview);
        }

        [HttpPost("token/{token}/start")]
        [Authorize(Roles = "Candidate,Admin")]
        public async Task<IActionResult> StartInterviewSession(string token)
        {
            var candidateId = GetCurrentUserId();
            var session = await _interviewService.StartSessionAsync(token, candidateId);

            return Ok(session);
        }

        [HttpPost("sessions/{sessionId:guid}/complete")]
        [Authorize(Roles = "Candidate,Admin")]
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
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> GetInterviewSessions(Guid interviewId)
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetInterviewSessionsAsync(interviewId, interviewerId);

            return Ok(result);
        }

        [HttpGet("sessions/{sessionId:guid}")]
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> GetInterviewSessionDetails(Guid sessionId)
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetInterviewSessionDetailsAsync(sessionId, interviewerId);

            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> GetMyInterviews()
        {
            var interviewerId = GetCurrentUserId();
            var result = await _interviewService.GetMineAsync(interviewerId);

            return Ok(result);
        }
    }
}
