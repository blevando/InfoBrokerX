using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    class InvoiceTransactions
    {
        public int Id { get; set; }
        public string? MatricNumber { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? ProgrammeId { get; set; }
        public string? ProgrammeName { get; set; }
        public string? InvoiceNumber { get; set; }   // DocEntry in SAP   PK(Primary Key)  
        public string? FeeType { get; set; }
        public string? InvoiceCode { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; } // Obtained from fees schedule table.
        public string? SessionId { get; set; }
        public string? SemesterId { get; set; }
        public PaymentStatusEnum PaymentStatus { get; set; }
    }

    public enum PaymentStatusEnum
    {
        UnPaid = 0,
        Paid = 1,
        PartiallyPaid = 2,
        OverPaid = 3
    }
}
