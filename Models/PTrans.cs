using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class PTrans
    {
        public long PaymentTransactionId { get; set; }

        public string PayerId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public int ProgrammeId { get; set; }

        public string Email { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public int FeeTypeId { get; set; }

        public string PaymentReference { get; set; } = string.Empty;

        public string? PaymentDescription { get; set; }

        public string PaymentChannel { get; set; } = string.Empty;

        public int SessionId { get; set; }

        public int SemesterId { get; set; }

        public string SessionSemester { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; }

        public string FeeTypeCode { get; set; } = string.Empty;

        public string? BankAccount { get; set; }

        public string? ApplicantBPCode { get; set; }

        public string? ApplicantAcceptBPCode { get; set; }

        public string? StudentBPCode { get; set; }

        public string? OrderNumber { get; set; }

    }
}
