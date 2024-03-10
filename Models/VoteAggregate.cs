using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class VoteAggregate
    {
        public string ElectionId { get; set; }
        public string Position { get; set; }
        public string CandidateId { get; set; }
        public int VoteCount { get; set; }
         
    }
}
