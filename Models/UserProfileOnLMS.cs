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


    public class UserRetrievedLMS
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Idnumber { get; set; }
        public int Firstaccess { get; set; }
        public int Lastaccess { get; set; }
        public string Auth { get; set; }
        public bool Suspended { get; set; }
        public bool Confirmed { get; set; }
        public string Lang { get; set; }
        public string Theme { get; set; }
        public string Timezone { get; set; }
        public int Mailformat { get; set; }
        public string Profileimageurlsmall { get; set; }
        public string Profileimageurl { get; set; }
    }

    public class LMSUserObject
    {
        public List<UserRetrievedLMS> Users { get; set; }
        public List<object> Warnings { get; set; }
    }




    public class UserProfileOnLMSError
    {
        
        public string? exception { get; set; }
        public string? errorcode { get; set; }

        public string? message { get; set; }

    }

}
