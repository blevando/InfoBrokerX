using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class CourseScheduleProfile
    {
        // https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=core_course_create_courses&moodlewsrestformat=json&courses[0][fullname]=Software Architecting&courses[0][shortname]=CSC154&courses[0][categoryid]=2&courses[0][idnumber]=CSC154&courses[0][summary]=CSC154 transitions students to programming on the UNIX machines. The class aims to teach students about computer systems from the hardware up to the source code. Topics include machine architecture (registers, I/O, basic assembly language), memory models (pointers, memory allocation, data representation), compilation (stack frames, semantic analysis, code generation), and basic concurrency (threading, synchronization).&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]=1689202800&courses[0][enddate]=1702422000&courses[0][visible]=1

        public int Id { get; set; }
        public string wsfunction { get; set; }
        public string wstoken { get; set; }
        public string LMSUrl { get; set; }
        public string moodlewsrestformat { get; set; }
        public int LMSCourseId { get; set; }
        public string roleid { get; set; }
        public string userid { get; set; }
        public string courseid { get; set; }
        public string StartDate { get;  set; }
        public string EndDate { get;  set; }
        public string ShortName { get;  set; }
        public string CategoryId { get; set; }
        public object CourseDescription { get;  set; }
        public string CourseTitle { get;  set; }
    }

}

