using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class GetDeleteCoursesProfile
    {

        public string? wstoken { get; set; }
        public string? LMSUrl { get; set; }
        public string? wsfunction { get; set; }
        public string? moodlewsrestformat { get; set; }
        public int courseid {  get; set; }  
       // public string? coursecode { get; set; }

    }
}
