﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class StudentProfile
    {
        public int Id { get; set; }
        public string MatricNumber { get; set; }

        public string LastName { get; set; }

        public string  FirstName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

    }
}
