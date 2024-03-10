using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class CandidateDto
    {
        public int id { get; set; }
        public int ElectionId { get; set; }
        public string? Position { get; set; }
        public string? Candidate { get; set; }
        public string? CandidateId { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public string? MembershipGrade { get; set; }
        public long VoteCount { get; set; }
        public string? Comment { get; set; }
        public string? Passport { get; set; }
        public string? Manifesto { get; set; }
        public string? YouTube { get; set; }
        public bool ActiveStatus { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
