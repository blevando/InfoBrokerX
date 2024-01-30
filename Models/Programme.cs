using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class Programme
    {
        public int ProgrammeId { get; set; }
        public string ProgrammeName { get; set; }
        public string ProgrammeCode { get; set; }
        public bool Status { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public string ApplicantBPCode { get; set; }
        public string StudentBPCode { get; set; }
        public string ApplicantAcceptBPCode { get; set; }
    }
}
