using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class MemberProfile
    {
        public int Id { get; set; }
        public string? MembershipNo { get; set; }

        public string? LastName { get; set; }

        public string?  FirstName { get; set; }

        public string? Email { get; set; }

     //   public string? Phone { get; set; }

    }
}
