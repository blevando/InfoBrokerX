using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class CourseSchedule
    {
        public int Id { get; set; }
        
        public string ExamPeriodCode { get; set; }
        public string ExamCode { get; set; }        
        public string ExamTitle { get; set; }
        public int ActiveStatus { get; set; }
        public int Provisioned { get; set; }
        public int LMSId { get; set; }


    }
}
