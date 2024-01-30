using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class PaymentInvoice
    {
        /// <summary>
        /// Payment ID
        /// </summary>
        public string CardCode { get; set; }
        /// <summary>
        /// formated datetime "yyyy-MM-Day
        /// </summary>
        public string DocDate  { get; set; } 
        /// <summary>
        /// Full Name of the payer
        /// </summary>

        public string NumAtCard { get; set; }
        /// <summary>
        /// Sequential Invoice Number
        /// </summary>
        public string U_PortalInvoiceNo { get; set; } // invoice number from the poral

        public DocumentLines DocumentLines { get; set; }

    }
}


 
