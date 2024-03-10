using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class GradeDetail
    {
        public int CourseId { get; set; }
        public string? Grade { get; set; }
        public string? RawGrade { get; set; }

       
    }

    public class GradesResponse
    {
        public List<GradeDetail> Grades { get; set; }
        public List<string> Warnings { get; set; }
    }
}
