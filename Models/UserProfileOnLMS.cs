using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CIPMBroker.Models
{
    public class UserProfileOnLMS
    { 
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public int FirstAccess { get; set; }
        public int LastAccess { get; set; }
        public bool Suspended { get; set; }
        public string? TimeZone { get; set; }
        public string? profileImageurlSmall { get; set; }

    }

}
