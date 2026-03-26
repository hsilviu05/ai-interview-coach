using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class CreateInterviewRequestDto
    {
        public string Title { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
    }
}
