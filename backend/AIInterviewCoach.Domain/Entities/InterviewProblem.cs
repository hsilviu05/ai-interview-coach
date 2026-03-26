using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Domain.Entities
{
    public class InterviewProblem
    {
        public Guid Id { get; set; }

        public Guid InterviewId { get; set; }
        public Interview Interview { get; set; } = null!;

        public Guid ProblemId { get; set; }
        public Problem Problem { get; set; } = null!;

        public int OrderIndex { get; set; }
        public int Points { get; set; }
    }
}