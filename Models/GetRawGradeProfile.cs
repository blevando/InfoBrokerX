using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class GetRawGradeProfile
    {

       
        public string? wstoken { get; set; }
        public string? LMSUrl { get; set; }
        public string? wsfunction { get; set; }
        public string? moodlewsrestformat { get; set; }
        public string? UserId { get; set; }

    }
}
