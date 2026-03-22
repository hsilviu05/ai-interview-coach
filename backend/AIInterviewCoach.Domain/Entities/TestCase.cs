using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIInterviewCoach.Domain.Entities
{
    public class TestCase
    {
        public Guid Id { get; set; }
        public Guid ProblemId { get; set; }
        public Problem Problem { get; set; } = null!;
        public string Input { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public bool IsHidden { get; set; } = false;
        public int OrderIndex { get; set; } = 0;

    }
}