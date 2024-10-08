using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class GeneralCourseRegistration
    {
        public int Id { get; set; }
        public string? LMSUserId { get; set; }
        public string? LMSCourseId { get; set; }
    }
}
