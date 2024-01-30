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
}
