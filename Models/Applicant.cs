using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoBroker.Models
{
    public class Applicant
    {
        public int ApplicantId { get; set; }
        public string ApplicantNo { get; set; }
        public string RegNo { get; set; }
        public string Title { get; set; }
        public string Surname { get; set; }
        public string OtherNames { get; set; }
        public string UserName { get; set; }
        public string MaritalStatus { get; set; }
        public string MaidenName { get; set; }
        public string Religion { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Nullable<System.DateTime> DateOfBirth { get; set; }
        public string PlaceOfBith { get; set; }
        public Nullable<int> CountryId { get; set; }
        public Nullable<int> StateId { get; set; }
        public Nullable<int> LGAId { get; set; }
        public string Town { get; set; }
        public Nullable<int> SessionId { get; set; }
        public Nullable<int> ApplicationBatchId { get; set; }
        public Nullable<int> ProgrammeId { get; set; }
        public int ModeOfEntryId { get; set; }
        public int ModeOfStudyId { get; set; }
        public Nullable<int> Faculty1Id { get; set; }
        public Nullable<int> Department1Id { get; set; }
        public Nullable<int> CourseOfStudy1Id { get; set; }
        public Nullable<int> Faculty2Id { get; set; }
        public Nullable<int> Department2Id { get; set; }
        public Nullable<int> CourseOfStudy2Id { get; set; }
        public Nullable<int> StudyCentreId { get; set; }
        public Nullable<int> ExamLocationId { get; set; }
        public string ApplicantUrl { get; set; }
        public string HighestQualification { get; set; }
        public bool HasAppliedBefore { get; set; }
        public bool IsSubmitted { get; set; }
        public Nullable<System.DateTime> SubmittedDate { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
        public Nullable<bool> IsAdmitted { get; set; }
        public string SignUpOTPToken { get; set; }
        public Nullable<bool> IsAccountVerified { get; set; }
    }


    public partial class Student
    {
        public int StudentId { get; set; }
        public string MatricNumber { get; set; }
        public string ApplicationRegNo { get; set; }
        public string Title { get; set; }
        public string Surname { get; set; }
        public string OtherNames { get; set; }
        public string UserName { get; set; }
        public string MaritalStatus { get; set; }
        public string MaidenName { get; set; }
        public string Religion { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Nullable<System.DateTime> DateOfBirth { get; set; }
        public Nullable<int> CountryId { get; set; }
        public Nullable<int> StateId { get; set; }
        public Nullable<int> LGAId { get; set; }
        public string Town { get; set; }
        public Nullable<int> StudyCentreId { get; set; }
        public int AcademicLevelId { get; set; }
        public int AdmittedLevelId { get; set; }
        public int PresentSessionId { get; set; }
        public int AdmittedSessionId { get; set; }
        public int ProgrammeId { get; set; }
        public int ModeOfStudyId { get; set; }
        public Nullable<int> ModeOfEntryId { get; set; }
        public int FacultyId { get; set; }
        public int DepartmentId { get; set; }
        public int CourseOfStudyId { get; set; }
        public Nullable<int> TeachingSubjectId { get; set; }
        public Nullable<int> StudentStatusId { get; set; }
        public bool IsNewStudent { get; set; }
        public bool IsMigrated { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
    }
}
