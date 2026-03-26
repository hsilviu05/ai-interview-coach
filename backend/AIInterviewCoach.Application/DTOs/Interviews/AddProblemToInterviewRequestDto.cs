using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Application.DTOs.Interviews
{
    public class AddProblemToInterviewRequestDto
    {
        public Guid ProblemId { get; set; }
        public int OrderIndex { get; set; }
        public int Points { get; set; }
    }
}