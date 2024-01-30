using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class Invoice
    {
        public int ID { get; set; }
        public string InvoiceNo { get; set; }
        public Nullable<decimal> Amount { get; set; }
        public string PayerName { get; set; }
        public string SessionName { get; set; }
        public Nullable<System.DateTime> DateGenerated { get; set; }
        public Nullable<bool> PostedToERP { get; set; }
        public Nullable<System.DateTime> DatePosted { get; set; }
        public string AccountCode { get; set; }
        public string PostedInvoicePayload { get; set; }
        public string ReceivedInvoicePayload { get; set; }
        public string PostedPaymentPayload { get; set; }
        public string ReceivedPaymentPayload { get; set; }
    }
}



