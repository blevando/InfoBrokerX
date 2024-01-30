

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class DocumentLines
    {
        public long LineNum { get; set; } = 0;
        public string? ItemCode { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }

    }
}