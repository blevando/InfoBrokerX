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

    public class CourseUnEnrolledProfile
    {
        internal int username;

        //https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=enrol_manual_unenrol_users&moodlewsrestformat=json&enrolments[0][userid]=636&enrolments[0][courseid]=12

        public string wsfunction { get; set; }
        public string wstoken { get; set; }
        public string LMSUrl { get; set; }
        public string moodlewsrestformat { get; set; }
        public string roleid { get; set; }
        public string userid { get; set; }
        public string courseid { get; set; }

    }
    public class SuspendUserProfile
    {
        internal int username;
        //https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=core_user_update_users&moodlewsrestformat=json&users[0][id]=832&users[0][suspend]=1
       
        public string wsfunction { get; set; }
        public string wstoken { get; set; }
        public string LMSUrl { get; set; }
        public string moodlewsrestformat { get; set; }      
        public string userid { get; set; }
        public string Suspend { get; set; }


    }
}
