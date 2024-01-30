using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class PaymentReceived
    {
        /// <summary>
        /// Payment ID
        /// </summary>
        public string CardCode { get; set; }
        /// <summary>
        /// formated datetime "yyyy-MM-Day
        /// </summary>
        public string DocDate { get; set; }

        /// <summary>
        /// Full Name of the customer
        /// </summary>
        public string U_CustName { get; set; }
        /// <summary>
        /// Full Name of the payer
        /// </summary>
        
        public string U_PortalReceiptNo { get; set; } // invoice number from the poral
        /// <summary>
        /// The Chatt of Account code in the destination ERP for the revenue under consideration
        /// </summary>
        public string TransferAccount { get; set; }
        
        /// <summary>
        /// This is the amount received in the transaction - sum being posted to the ERP
        /// </summary>
        public string TransferSum { get; set; }

        public  PaymentInvoices PaymentInvoices { get; set; }

    }
}
