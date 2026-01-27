using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class CourseRegistration
    { 
        public int Id { get; set; }
        public string MatricNumber { get; set; }
        public string CourseCode { get; set; }
        public int SessionId { get; set; }
        public int SchoolSemesterId { get; set; }
        public string ShortName { get;  set; }
        public int LMSUserId { get;  set; }
    }
    public class CourseUnEnrolled
    {
        public int Id { get; set; }
        public string? LMSUserId { get; set; }
        public string? LMSCourseId { get; set; }
    }
    public class Suspendmodel
    {
        public int StudentId { get; set; }
        public string? LMSUserId { get; set; }
       
    }
}
