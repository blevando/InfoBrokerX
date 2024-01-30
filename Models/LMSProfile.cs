using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class LMSProfile
    {

        public int Id { get; set; }
        public string wstoken { get; set; }
        public string LMSUrl { get; set; }
        public string wsfunction { get; set; }
        public string moodlewsrestformat { get; set; }
        public int createpassword { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string auth  { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string email { get; set; }
        public int maildisplay { get; set; }
        public string idnumber { get; set; }
        public string usersLang { get; set; }
        
    }
}
