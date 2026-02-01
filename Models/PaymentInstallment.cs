using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    class PaymentInstallment
    {
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }   // DocEntry in SAP   PK(Primary Key)   
        public int InstallmentNumber { get; set; }          // LineNum in SAP   PK(Primary Key)
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string? PaymentCode { get; set; }    // PaymentCode in SAP
        public  string? PaymentReference { get; set; }
        public  string? CustomerName { get; set; } // NumAtCard in SAP
        public  string? CardCode { get; set; } // 
        public string? BankAccount { get; set; } // BankAccount in SAP
        public string? PortalReceiptNo { get; set; } // Local reference from the portal
        public int PostStatus { get; set; }
    }
}
