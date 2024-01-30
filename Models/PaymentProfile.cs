using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class PaymentProfile
    {
        public int Id { get; internal set; }
        public string PayerId { get;  set; }
        public string FullName { get;  set; }
        public int ProgrammeId { get;   set; }
        public string Email { get;   set; }
        public string? Amount { get; set; }
        public int FeeTypeId { get; set; }
        public string PaymentReference { get; set; }
        public string PaymentDescriptipon { get; set; }
        public string PaymentChannel { get; set; }
        public int SessionId { get; set; }
        public int SemesterId { get; set; }
        public string SessionSemester { get; set; }
        public string PaymentDate { get; set; }
        public string FeeTypeCode { get; set; }
        public string BankAccount { get; set; }
        public string CardCode { get; internal set; }
        public string ItemCode { get; internal set; }
    }
}
