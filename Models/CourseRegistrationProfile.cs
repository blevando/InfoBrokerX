using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class CourseRegistrationProfile
    {
        internal int username;

        // https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=enrol_manual_enrol_users&moodlewsrestformat=json&enrolments[0][roleid]=5&enrolments[0][userid]=18&enrolments[0][courseid]=3

        public string wsfunction { get; set; }
        public string wstoken { get; set; }
        public string LMSUrl { get; set; }
        public string moodlewsrestformat { get; set; }
        public string roleid { get; set; }
        public string userid { get; set; }
        public string courseid { get; set; }
       
    }
}
