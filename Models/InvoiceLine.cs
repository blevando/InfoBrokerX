using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    class InvoiceLine
    {
        public int Id { get; set; }
        public string? InvoiceNumber { get; set; }   // DocEntry in SAP   PK(Primary Key)
        public int LineNumber { get; set; }          // LineNum in SAP   PK(Primary Key)
        [Column(TypeName = "decimal(18,2)")]    
        public decimal Amount  { get; set; }
        public string? Description { get; set; }
    }
}
