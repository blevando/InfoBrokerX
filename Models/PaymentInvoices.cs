using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class PaymentInvoices
    {
        public string LineNumber { get; set; }
        public string InvoiceType { get; set; }
        public string DocEntry { get; set; }
        public double SumApplied { get; set; }

    }
}
