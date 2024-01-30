using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class CourseSchedule
    {
        public int Id  { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public string YearId { get; set; }
        public int SemesterId { get; set; }
        public int LMSCourseId { get; set; }
     

    }
}
