﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class ErpSignInBody
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CompanyDB { get; set; }
    }
}
