using System.Security.Claims;
using AIInterviewCoach.Application.DTOs.Problems;
using AIInterviewCoach.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIInterviewCoach.API.Controllers
{
    [ApiController]
    [Route("api/problems")]
    [Authorize]
    public class ProblemsController : ControllerBase
    {
        private readonly IProblemService _problemService;

        public ProblemsController(IProblemService problemService)
        {
            _problemService = problemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllProblems()
        {
            var problems = await _problemService.GetAllProblemsAsync();
            return Ok(problems);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetProblemById(Guid id)
        {
            var problem = await _problemService.GetProblemByIdAsync(id);
            if (problem == null)
                return NotFound(new { message = "Problem not found." });

            return Ok(problem);
        }

        [HttpPost]
        [Authorize(Roles = "Interviewer,Admin")]
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
        [Authorize(Roles = "Interviewer,Admin")]
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
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> DeleteProblem(Guid id)
        {
            var currentUserId = GetCurrentUserId();
            var deleted = await _problemService.DeleteProblemAsync(id, currentUserId);

            if (!deleted)
                return NotFound(new { message = "Problem not found." });

            return NoContent();
        }

        [HttpPost("{problemId:guid}/testcases")]
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> AddTestCase(Guid problemId, [FromBody] CreateTestCaseRequestDto createTestCaseRequest)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var testCase = await _problemService.AddTestCaseAsync(problemId, createTestCaseRequest);
            return Ok(testCase);
        }

        [HttpGet("{problemId:guid}/testcases")]
        [Authorize(Roles = "Interviewer,Admin")]
        public async Task<IActionResult> GetTestCases(Guid problemId, [FromQuery] bool includeHidden = false)
        {
            var testCases = await _problemService.GetTestCasesAsync(problemId, includeHidden);
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
    }
}