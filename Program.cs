namespace InfoBroker
{
    using InfoBroker.Models;
    using log4net;
    using log4net.Config;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.OpenApi.Models;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestSharp; // Nugget Version = 105.2.3 - This is what worked
    //using RestSharp.Authenticators;
    using System.Collections;
    using System.Data;
    using System.Data.SqlClient;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Security;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading;
    using System.Threading.Tasks;



    public class Program
    {
        #region
        static int threadCount;
        static bool isApplicationProcessing;



        static int lnNumber = 0;
        static long movingNum = 0;
        #endregion

        private static ILog _log4net = log4net.LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            isApplicationProcessing = true;

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("Log4Net.config"));

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();        
            var configsession = configBuilder.GetSection("AppSettings");
            string organizationName = configsession.GetSection("Organization").Value.Trim();

            Console.Title = $"InfoBroker for {organizationName} runing since {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";

            _log4net.Info(organizationName);

            Thread Credentials = null;

            Thread UpdatePayment = null;

            Thread RegisterStudents = null;

            Thread Email = null;
            Thread CreateCourse;


            //Thread updateDepartments;

            //Thread updateFaculties;

            //Thread UpdatePrograms;



            // This tread is for ....
            Credentials = new Thread(new ParameterizedThreadStart(CredentialsHandler));
            Credentials.Start();
            Interlocked.Increment(ref threadCount);



            UpdatePayment = new Thread(new ParameterizedThreadStart(PaymentUpdateHandler));
            UpdatePayment.Start();
            Interlocked.Increment(ref threadCount);

            RegisterStudents = new Thread(new ParameterizedThreadStart(CourseRegistrationHandler));
            RegisterStudents.Start();
            Interlocked.Increment(ref threadCount);



            CreateCourse = new Thread(new ParameterizedThreadStart(CreateCourseHandler));
            CreateCourse.Start();


            Interlocked.Increment(ref threadCount);


            //Email = new Thread(new ParameterizedThreadStart(SendEmail));
           // Email.Start();
           // Interlocked.Increment(ref threadCount);


            //updateDepartments = new Thread(new ParameterizedThreadStart(DepartmentHandler));
            //updateDepartments.Start();
            //Interlocked.Increment(ref threadCount);


            //updateFaculties = new Thread(new ParameterizedThreadStart(FacultyHandler));
            //updateFaculties.Start();
            //Interlocked.Increment(ref threadCount);


            //UpdatePrograms = new Thread(new ParameterizedThreadStart(ProgramsHandler));
            //UpdatePrograms.Start();
            //Interlocked.Increment(ref threadCount);



            while (!isApplicationProcessing == false)

            {
                _log4net.Info($"Main Tread is running {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                
                Thread.Sleep(3000);
            }

        }

        private static void SendEmail(object? obj)
        {
            throw new NotImplementedException();
        }

        private static void CreateCourseHandler(object? obj)
        {
            _log4net.Info("Course Registration Module fired");

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            SqlConnection cnn = new SqlConnection(connectionstring);
            cnn.Open();

            try
            {

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {

                        SqlCommand cmd = new SqlCommand("Select [srn],[CourseCode], [CourseTitle],[YearId],[SemesterId] from [EduCourseSchedule] where [isTransmitedToLMS]=0", cnn);


                        SqlDataReader dr = cmd.ExecuteReader();
                        List<CourseSchedule> schedules = new List<CourseSchedule>();
                        if (dr.HasRows == true)
                        {
                            CourseSchedule schedule = null;
                            while (dr.Read() == true)
                            {
                                schedule = new CourseSchedule();
                                
                                schedule.Id = int.Parse(dr.GetValue(0).ToString());
                                schedule.CourseCode = dr.GetString(1);
                                schedule.CourseTitle = dr.GetString(2);
                                schedule.YearId =  dr.GetString(3);
                                schedule.SemesterId = int.Parse(dr.GetValue(4).ToString());
                                schedule.LMSCourseId = 0; // Zero is passed because it is not yet known 

                                schedules.Add(schedule);
                            }

                        }

                        dr.Close();
                        cmd.Dispose();



                        if (schedules.Count > 0)
                        {
                            // Send to LMS                      

                            foreach (CourseSchedule schedule in schedules)
                            {

                                // Send to LMS

                                //string a = DateTime.Now.ToString();
                                //a = a.ToInteger();




                                CourseScheduleProfile profile = GetCourseScheduleProfile(schedule);

                                CreateCategory(profile.CategoryId);

                                
                                int LMSid = CreateCourse(profile);

                                if (LMSid > 0)
                                {


                                    // Update Student Table 
                                    cmd = new SqlCommand($"Update EduCourseSchedule set [LMSCourseId] = {LMSid}, [isTransmitedToLMS]=1 where [srn] ={schedule.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();

                                }

                            }

                        }


                        Thread.Sleep(20000);
                    }
                    else
                    {
                        try
                        {
                            cnn.Open();
                          _log4net.Warn("Previous connection expired and was reestablished");

                        }
                        catch (Exception xe)
                        {
                           _log4net.Error(xe.Message);
                        }

                        Thread.Sleep(1000);
                    }




                    
                  _log4net.Info(  $"Registration Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}"  );
                    
                    Thread.Sleep(3000);
                }

            }
            catch (Exception xe)
            {
                _log4net.Error(xe.Message);
            }

        }

        private static int CreateCourse(CourseScheduleProfile profile)
        {

            int LMSid = 0;

            // $"{registration.CourseCode.Trim().ToUpper()}_{registration.SessionId.ToString()}_{registration.SchoolSemesterId.ToString()}";

            var client = new HttpClient();

            //https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=core_course_create_courses&moodlewsrestformat=json&courses[0][fullname]=Software Architecting&courses[0][shortname]=CSC154&courses[0][categoryid]=2&courses[0][idnumber]=CSC154&courses[0][summary]=CSC154 transitions students to programming on the UNIX machines. The class aims to teach students about computer systems from the hardware up to the source code. Topics include machine architecture (registers, I/O, basic assembly language), memory models (pointers, memory allocation, data representation), compilation (stack frames, semantic analysis, code generation), and basic concurrency (threading, synchronization).&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]=1689202800&courses[0][enddate]=1702422000&courses[0][visible]=1
            //wsfunction=core_course_create_courses&moodlewsrestformat=json&courses[0][fullname]=Software Architecting&courses[0][shortname]=CSC154&courses[0][categoryid]=2&courses[0][idnumber]=CSC154&courses[0][summary]=CSC154 transitions students to programming on the UNIX machines. The class aims to teach students about computer systems from the hardware up to the source code. Topics include machine architecture (registers, I/O, basic assembly language), memory models (pointers, memory allocation, data representation), compilation (stack frames, semantic analysis, code generation), and basic concurrency (threading, synchronization).&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]=1689202800&courses[0][enddate]=1702422000&courses[0][visible]=1


            string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&courses[0][fullname]={profile.CourseTitle}&courses[0][shortname]={profile.ShortName}&courses[0][categoryid]={profile.CategoryId}&courses[0][idnumber]={profile.courseid}&courses[0][summary]={profile.CourseDescription}&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]={profile.StartDate}&courses[0][enddate]={profile.EndDate}&courses[0][visible]=1";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            var response = client.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            string aa = ProfileTransmitedFeedBack.ToString();

            List<CreateCourseResponse> usersData = new List<CreateCourseResponse>();
            _log4net.Info(aa);
            try
            {

                if (aa.Contains("exception") == false)
                {
                    usersData = JsonConvert.DeserializeObject<List<CreateCourseResponse>>(ProfileTransmitedFeedBack);
                }

            }
            catch (Exception)
            {

                throw;
            }


            if (usersData.Count > 0)
            {

                // CreateCourseResponse res = (CreateCourseResponse)usersData;

                profile.LMSCourseId = usersData[0].Id;
                LMSid = profile.LMSCourseId;



            }


            return LMSid;
        }

        private static CourseScheduleProfile GetCourseScheduleProfile(CourseSchedule schedule)
        {


            //Doues the CategoryID Exists? Yes=Move On otherwise create it

           




            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSCreateCourse");


            CourseScheduleProfile profile = new CourseScheduleProfile();

            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();

            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

            profile.wsfunction = configsession.GetSection("WSfunction").Value.Trim();

            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();

            profile.StartDate = configsession.GetSection("StartDate").Value.Trim().ToInteger();

            profile.EndDate = configsession.GetSection("EndDate").Value.Trim().ToInteger();



            // Find out which category is current
            profile.CategoryId  = $"{schedule.YearId.LastFourCharacters()}{schedule.SemesterId}";     // configsession.GetSection("CategoryId").Value.Trim();
             

            profile.ShortName = $@"{schedule.CourseCode.ToUpper()}_{schedule.YearId.LastTwoCharacters()}{schedule.SemesterId}";
            profile.courseid = schedule.CourseCode.ToUpper();

            profile.CourseDescription = $@"This course is {schedule.CourseTitle} ...";
            profile.CourseTitle = $"{profile.courseid}-{schedule.CourseTitle}";



            return profile;

        }

        private static void CreateCategory(string categoryId)
        {


            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSCreateCourse");


            

            string wstoken = configsession.GetSection("LMSToken").Value.Trim();

           string LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

          string wsfunction = configsession.GetSection("WSfunction").Value.Trim();

            string moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();

            

            var client = new HttpClient();

            string apiUrl = $@"https://class.jhu.edu.ng/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_course_get_categories&moodlewsrestformat=json&criteria[0][key]=id&criteria[0][value]={categoryId}";

             
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            var response = client.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            string aa = ProfileTransmitedFeedBack.ToString();

            // Create it if there is error





            List<CreateCourseResponse> usersData = new List<CreateCourseResponse>();
            _log4net.Info(aa);
            try
            {

                if (aa.Contains("exception") == false)
                {
                   // usersData = JsonConvert.DeserializeObject<List<CreateCourseResponse>>(ProfileTransmitedFeedBack);
                }

            }
            catch (Exception)
            {

                throw;
            }



        }

        private static void CourseRegistrationHandler(object? obj)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            SqlConnection cnn = new SqlConnection(connectionstring);
            cnn.Open();

            try
            {

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {

                        SqlCommand cmd = new SqlCommand(@"Select CR.CourseRegistrationId,CR.MatricNumber,CR.CourseCode,CR.SessionId,CR.SchoolSemesterId,ST.LMSUserId from CourseRegistration CR join Students ST  on CR.MatricNumber = ST.MatricNumber where CR.isRegisteredToLMS=0", cnn);


                        SqlDataReader dr = cmd.ExecuteReader();
                        List<CourseRegistration> registrations = new List<CourseRegistration>();
                        if (dr.HasRows == true)
                        {
                            var configBuilder1 = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
                            var configsession1 = configBuilder1.GetSection("LMSRegistration");

                            string YearID = configsession1.GetSection("CurrentYearId").Value.Trim();//.ToInteger();
                            string SemesterId = configsession1.GetSection("CurrentSemesterId").Value.Trim();

                            CourseRegistration registration = null;
                            while (dr.Read() == true)
                            {
                                registration = new CourseRegistration();

                                registration.Id = int.Parse(dr.GetValue(0).ToString());
                                registration.MatricNumber = dr.GetString(1).Split(new char[] { '/', ' ', '-', '_' }).Aggregate((a, b) => (a + b)).ToLower();
                                
                                registration.CourseCode = dr.GetString(2);
                                registration.SessionId = int.Parse(dr.GetValue(3).ToString());
                                registration.SchoolSemesterId = int.Parse(dr.GetValue(4).ToString());
                                registration.LMSUserId = int.Parse(dr.GetValue(5).ToString());
                                registration.ShortName = $@"{registration.CourseCode.ToUpper()}_{YearID}{SemesterId}";                                
                                
                                registrations.Add(registration);
                            }

                            // This region was to test registration
                            #region /// To be deleted 

                            //registration = new CourseRegistration();

                            //registration.Id = 1;
                            //registration.MatricNumber = "o";

                            //registration.CourseCode = "TEST821";
                            //registration.SessionId = 23;
                            //registration.SchoolSemesterId = 1;
                            //registration.LMSUserId = 22;
                            //registration.ShortName = "TEST821_231";

                            //registrations.Add(registration);


                            #endregion



                        }

                        dr.Close();
                        cmd.Dispose();



                        if (registrations.Count > 0)
                        {
                            // Send to LMS                      

                            foreach (CourseRegistration registration in registrations)
                            {

                                // Send to LMS


                                CourseRegistrationProfile profile = GetCourseRegistrationProfile(registration,cnn);

                                if (RegisterStudent(profile) == true)

                                { // Update Student Table 

                                    cmd = new SqlCommand($"Update CourseRegistration set [isRegisteredToLMS]=1 where [CourseRegistrationId] ={registration.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();

                                }

                            }

                        }


                        Thread.Sleep(20000);
                    }
                    else
                    {
                        try
                        {
                            cnn.Open();
                            _log4net.Warn("Previous connection expired and was reestablished");

                        }
                        catch (Exception xe)
                        {
                           _log4net.Error(xe.Message);
                        }

                        Thread.Sleep(1000);
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    _log4net.Info($"Registration Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                    Console.ResetColor();
                    Thread.Sleep(3000);
                }

            }
            catch (Exception xe)
            {
                _log4net.Info(xe.Message);
            }


        }

        private static bool RegisterStudent(CourseRegistrationProfile profile)
        {
            bool ret = false;

            try
            {
                // 
                var client = new HttpClient();
                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&enrolments[0][roleid]={profile.roleid}&enrolments[0][userid]={profile.userid}&enrolments[0][courseid]={profile.courseid}";

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (ProfileTransmitedFeedBack != null)
                {
                    if (ProfileTransmitedFeedBack.ToString().Contains("exception") != true)
                    {
                        var usersData = JsonConvert.DeserializeObject<List<UserLMS>>(ProfileTransmitedFeedBack);


                        if (usersData == null)
                        {

                            ret = true;

                        }
                    }
                    else
                    {
                        _log4net.Warn($"UserID {profile.userid} - {profile.username} - {ProfileTransmitedFeedBack.ToString()}");
                    }

                }

            }
            catch (Exception ex)
            {

                _log4net.Error(ex.Message);
            }


            return ret;
        }

        private static CourseRegistrationProfile GetCourseRegistrationProfile(CourseRegistration registration, SqlConnection cnn)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSRegistration");


            CourseRegistrationProfile profile = new CourseRegistrationProfile();

            profile.wstoken = configsession.GetSection("LMSRegistrationToken").Value.Trim();

            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

            profile.wsfunction = configsession.GetSection("ProfileWSfunction").Value.Trim();

            profile.roleid = configsession.GetSection("RoleId").Value.Trim();

            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();

            string YearId = configsession.GetSection("CurrentYearId").Value.Trim();
            string SemesterId = configsession.GetSection("CurrentSemesterId").Value.Trim();


            string coursecode = registration.CourseCode.Replace("1f", "1|").Replace("2f", "2|").Replace("1e", "1|").Replace("2e", "2|");
            coursecode = coursecode.Split(new char[] { '|' })[0];

            SqlCommand cmd = new SqlCommand($@"select LMSCourseId from EduCourseSchedule where CourseCode = '{coursecode}' and SemesterId={SemesterId} and YearId = {YearId}", cnn);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows==true)
            {
                if (dr.Read() ==true)
                {
                    profile.courseid = int.Parse(dr.GetValue(0).ToString()).ToString();
                } 

            }

            dr.Close();
            cmd.Dispose();            

            profile.userid = registration.LMSUserId.ToString();    // registration.MatricNumber.Split(new char[] { '/', ' ', '-', '_' }).Aggregate((a, b) => (a + b)).ToLower();
            return profile;

        }

        private static void PaymentUpdateHandler(object? source)
        {

            movingNum = long.Parse(DateTime.Now.ToString("yyyyMMddHHmm"));

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();

            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();


            var ERPconfigsession = configBuilder.GetSection("ERPSettings");

            string authenticationEndPoint = ERPconfigsession.GetSection("AuthRUL").Value.Trim();

            string invoiceEndPoint = ERPconfigsession.GetSection("InvoiceEndPoint").Value.Trim();

            string paymentEndPoint = ERPconfigsession.GetSection("PaymentEndPoint").Value.Trim();

            string BPCodeApplicant = ERPconfigsession.GetSection("BPCodeApplicant").Value.Trim().ToUpper();
            string BPCodeMasters = ERPconfigsession.GetSection("BPCodeMasters").Value.Trim().ToUpper();
            string BPCodePG = ERPconfigsession.GetSection("BPCodePG").Value.Trim().ToUpper();

            // BP Codes
            const string application = "JHU-APPL";
            const string masters = "JHU-TUIT";
            const string pg = "JHU-PGDT";

            try
            {

                SqlConnection cnn = new SqlConnection(connectionstring);
                cnn.Open();
                while (!isApplicationProcessing == false)

                {


                    if (cnn.State == ConnectionState.Open)
                    {

                        // Stage 1 - Are there pain transactions
                        SqlCommand cmd = new SqlCommand($"Select PT.PaymentTransactionId,PT.PayerId, PT.FullName, PT.ProgrammeId,PT.Email,  PT.Amount, PT.FeeTypeId, PT.PaymentReference, PT.PaymentDescription, PT.PaymentChannel, PT.SessionId, PT.SemesterId, PT.SessionSemester, PT.PaymentDate, FT.FeeTypeCode,FT.BankAccount, PG.ApplicantBPCode from PaymentTransaction PT Join FeeType FT on PT.FeeTypeId=FT.FeeTypeId Join Programme PG on PT.ProgrammeId=PG.ProgrammeId where PT.isTransmitedToERP = 0", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();

                        List<PaymentProfile> Paids = new List<PaymentProfile>();

                        if (dr.HasRows == true)
                        {
                            PaymentProfile Paid = new PaymentProfile();

                            while (dr.Read() == true)
                            {
                                Paid.Id = int.Parse(dr.GetValue(0).ToString());
                                Paid.PayerId = dr.GetString(1);
                                Paid.FullName = dr.GetString(2);
                                Paid.ProgrammeId = int.Parse(dr.GetValue(3).ToString());
                                Paid.Email = dr.GetString(4); // It is not needed
                                Paid.Amount = dr.GetValue(5).ToString();
                                Paid.FeeTypeId = int.Parse(dr.GetValue(6).ToString());
                                Paid.PaymentReference = dr.GetString(7);
                                Paid.PaymentDescriptipon = dr.GetString(8);
                                Paid.PaymentChannel = dr.GetString(9);
                                Paid.SessionId = int.Parse(dr.GetValue(10).ToString());
                                Paid.SemesterId = int.Parse(dr.GetValue(11).ToString());
                                Paid.SessionSemester = dr.GetString(12);
                                Paid.PaymentDate = dr.GetDateTime(13).ToString("yyyy-MM-dd HH:mm:ss.fff");
                                Paid.FeeTypeCode = dr.GetString(14);
                                Paid.BankAccount = dr.GetString(15);
                                Paid.ItemCode = dr.GetString(16);

                                switch (Paid.FeeTypeCode.Trim().ToUpper())
                                {

                                    case application:
                                        Paid.CardCode = BPCodeApplicant;
                                        break;

                                    case masters:
                                        Paid.CardCode = BPCodeMasters;
                                        break;

                                    case pg:
                                        Paid.CardCode = BPCodePG;
                                        break;

                                    default:
                                        Paid.CardCode = BPCodeMasters;
                                        break;
                                }


                               
                                Paids.Add(Paid);
                            }
                        }
                        dr.Close();
                        cmd.Dispose();

                        // Stage 2 = login to the ERP if there is paid transactions to process

                        if (Paids.Count > 0)
                        {

                            //Stage 3, Login to the db is there is no login currently 
                            // Loo=p through paid - 

                            var signinclient = new RestClient(authenticationEndPoint); // The Login endpoint
                                                                                       // signinclient.Timeout = -1;
                            var signinrequest = new RestRequest("", Method.POST);

                            ErpSignInBody signInBody = new ErpSignInBody();
                            signInBody.UserName = ERPconfigsession.GetSection("UserName").Value.Trim();
                            signInBody.Password = ERPconfigsession.GetSection("Passw").Value.Trim();
                            signInBody.CompanyDB = ERPconfigsession.GetSection("CompanyDb").Value.Trim();

                            var signIn = System.Text.Json.JsonSerializer.Serialize(signInBody) + "\n" + @"";  // 

                            // This block was used to supress the certificate authentication error
                            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
                            ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
                            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;


                            signinrequest.AddParameter("application/json", signIn, ParameterType.RequestBody);
                            IRestResponse signinresponse = signinclient.Execute(signinrequest);
                           _log4net.Info(signinresponse.Content.ToString());

                            if (signinresponse.ResponseStatus == ResponseStatus.Completed )
                            {
                                // Prepare the invoice and send

                                //   string someJson = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""NumAtCard"":""" + studentName + @""",""U_PortalInvoiceNo"":""" + invoicenumber + @""",""DocumentLines"": [{""LineNum"": " + 0 + @",""ItemCode"":""" + ItemCode + @""",""Quantity"": " + 1 + @", ""Price"": " + Convert.ToInt32(model.Amount) + @"}]}";


                                foreach (PaymentProfile payment in Paids)
                                {

                                    // Stage 4, prepare invoice and push to erp and get responce
                                    lnNumber = lnNumber + 1;
                                    if (lnNumber > 100)
                                    {
                                        lnNumber = 1;
                                    }

                                    movingNum = movingNum + 1;


                                    DocumentLines doc = new DocumentLines();
                                    doc.Price = double.Parse(payment.Amount);
                                    doc.Quantity = 1;
                                    doc.LineNum = lnNumber;
                                    doc.ItemCode = payment.ItemCode;

                                    // Switch Structure is hidden in the rejion bracket
                                    #region


                                    #endregion

                                    // Create the invoice
                                   // var docs = Array.Empty<DocumentLines>();

                                   // docs.Append(doc);

                                    PaymentInvoice invoice = new PaymentInvoice();
                                    invoice.NumAtCard = payment.FullName;
                                    // invoice.DocumentLines =  doc;
                                    invoice.DocumentLines = doc;
                                    invoice.U_PortalInvoiceNo = $"JHU-{DateTime.Now.ToString("00yy")}-{payment.Id}"; //    $"JHU-{movingNum.ToString().Substring(0, 4)}-{movingNum.ToString().Substring(4, movingNum.ToString().Length - 4)}"; ; // to be handled
                                    invoice.CardCode = payment.CardCode;

                                    _log4net.Info($"Invoice Number: {invoice.U_PortalInvoiceNo} ----- Line Number={doc.LineNum} - Payer ID= {payment.PayerId}");



                                    invoice.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");// HH:mm:ss.fff");

                                    // Then serialize it

                                    var invoiceData = System.Text.Json.JsonSerializer.Serialize(invoice) + "\n" + @"";  // 

                                    _log4net.Info(invoiceData);


                                    var B1session = signinresponse.Cookies.Where(a => a.Name == "B1SESSION").Select(a => a.Value).FirstOrDefault();
                                    var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();
                                    string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();

                                    var invoiceclient = new RestClient(invoiceEndPoint);
                                    invoiceclient.Timeout = -1;
                                    var invoicerequest = new RestRequest(Method.POST); // It is a post request 

                                    invoicerequest.AddHeader("Content-Type", "application/json");
                                    //invoicerequest.AddHeader("Cookie", "B1SESSION=cecf3b76-4dc9-11ec-8000-005056010273; ROUTEID=.node8");
                                    invoicerequest.AddHeader("Cookie", "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString());
                                    invoicerequest.AddCookie("B1SESSION", B1session.ToString());
                                    invoicerequest.AddCookie("ROUTEID", RouteID.ToString());

                                    invoicerequest.AddParameter("application/json", invoiceData, ParameterType.RequestBody);
                                    IRestResponse invoiceresponse = invoiceclient.Execute(invoicerequest);


                                   _log4net.Info(invoiceresponse.Content);


                                    if (invoiceresponse.ResponseStatus == ResponseStatus.Completed)
                                    {
                                        // This means that the invoice was created successfully, then send the Payment - 
                                        // string p = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""U_CustName"":""" + studentName + @""",""U_PortalReceiptNo"":""" + paymentnumber + @""",""TransferAccount"" :""" + "123301" + @""",""TransferSum"" : " + Convert.ToInt32(model.Amount) + @",""PaymentInvoices"": [{""LineNum"": " + 0 + @",""InvoiceType"":""" + "it_Invoice" + @""",""DocEntry"": " + docEntry + @", ""SumApplied"": " + Convert.ToInt32(model.Amount) + @"}]}";

                                        PaymentReceived received = new PaymentReceived();

                                        PaymentInvoices individualInvoice = new PaymentInvoices();

                                        individualInvoice.SumApplied = invoice.DocumentLines.Price;
                                        individualInvoice.DocEntry = payment.FeeTypeId.ToString();
                                        individualInvoice.InvoiceType = payment.FeeTypeCode;
                                        individualInvoice.SumApplied = double.Parse(payment.Amount);
                                        individualInvoice.LineNumber = invoice.DocumentLines.LineNum.ToString();

                                        // Now we add in
                                        received.PaymentInvoices = individualInvoice;

                                        received.U_CustName = invoice.NumAtCard;
                                        received.CardCode = invoice.CardCode;
                                        received.TransferAccount = payment.BankAccount;
                                        received.TransferSum = payment.Amount;
                                        received.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");
                                        received.U_PortalReceiptNo = invoice.U_PortalInvoiceNo.Replace("JHU","RPT");
                                        

                                        // We convert the received to JSON

                                        var paymentData = System.Text.Json.JsonSerializer.Serialize(received) + "\n" + @"";

                                        _log4net.Info(paymentData);

                                        var paymentclient = new RestClient(paymentEndPoint);
                                        paymentclient.Timeout = -1;
                                        var paymentrequest = new RestRequest(Method.POST);
                                        paymentrequest.AddHeader("Content-Type", "application/json");
                                        //invoicerequest.AddHeader("Cookie", "B1SESSION=cecf3b76-4dc9-11ec-8000-005056010273; ROUTEID=.node8");
                                        paymentrequest.AddHeader("Cookie", "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString());
                                        paymentrequest.AddCookie("B1SESSION", B1session.ToString());
                                        paymentrequest.AddCookie("ROUTEID", RouteID.ToString());

                                        paymentrequest.AddParameter("application/json", paymentData, ParameterType.RequestBody);
                                        IRestResponse paymentresponse = paymentclient.Execute(paymentrequest);
                                         



                              

                                        _log4net.Info(paymentresponse.Content);

                                        if (paymentresponse.ResponseStatus == ResponseStatus.Completed && paymentresponse.Content.Contains("error") == false)
                                        {

                                            cmd = new SqlCommand($"Update PaymentTransaction set isTransmitedToERP=1 where PaymentTransactionId ={payment.Id}", cnn);
                                            cmd.ExecuteNonQuery();
                                            cmd.Dispose();

                                            // Update the database : Payment Transaction Table, set isTransmittedToERP = 1 for the payment.Id field

                                        }
                                        else
                                        {
                                            Thread.Sleep(120000);
                                        }


                                    }

                                }

                                // Stage 5, push payment to the invoice 

                                    // Stage 6, update the PaymentTransaction Table

                            }


                        }
                        else
                        {
                            cnn.Dispose();
                            cnn = new SqlConnection(connectionstring);
                            cnn.Open();

                            Thread.Sleep(20000);
                         _log4net.Info("The database reconnected");

                        }



                    }


                    // Try end

                }
            }
            catch (Exception ex)
            {
               _log4net.Error(ex.Message);
                Thread.Sleep(120000);
            }



        }

        /// <summary>
        /// SSL Security handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static void FacultyHandler(object? source)
        {

            while (!isApplicationProcessing == false)

            {
               
                _log4net.Info($"Faculty thread in progress");
             

                Thread.Sleep(4000);
            }
        }

        private static void DepartmentHandler(object? sourcej)
        {
            while (!isApplicationProcessing == false)

            {
                 
                _log4net.Info($"Department thread in progress");
               

                Thread.Sleep(4000);
            }
        }

        private static void LecturerHandler(object? source)
        {
            while (!isApplicationProcessing == false)

            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine($"Lecturer {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                Console.ResetColor();

                Thread.Sleep(4000);
            }
        }

        private static void CourseHandler(object? source)
        {



            while (!isApplicationProcessing == false)

            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"Course {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                Console.ResetColor();

                Thread.Sleep(4000);
            }

        }

        private static void CredentialsHandler(object? source)
        {

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            SqlConnection cnn = new SqlConnection(connectionstring);
            cnn.Open();


            try
            {

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {

                        SqlCommand cmd = new SqlCommand("Select [StudentId],[MatricNumber],[Surname],[OtherNames],[Email],[Phone] from [Students] where [IdTransmitedToLMS]=0", cnn);

                        SqlDataReader dr = cmd.ExecuteReader();
                        List<StudentProfile> students = new List<StudentProfile>();
                        if (dr.HasRows == true)
                        {

                            StudentProfile student = null;
                            while (dr.Read() == true)
                            {
                                student = new StudentProfile();

                                student.Id = int.Parse(dr.GetValue(0).ToString());
                                student.MatricNumber = dr.GetString(1);
                                student.LastName = dr.GetString(2);
                                student.FirstName = dr.GetString(3);
                                student.Email = dr.GetString(4);
                                student.Phone = dr.GetString(5);

                                students.Add(student);

                            }



                        }

                        dr.Close();
                        cmd.Dispose();

                        List<StudentProfile> registeredStudents = new List<StudentProfile>();

                        if (students.Count > 0)
                        {
                            foreach (StudentProfile student in students)
                            {
                                cmd = new SqlCommand($"Select [MatricNumber] from [CourseRegistration] where [MatricNumber]='{student.MatricNumber}'", cnn);

                                dr = cmd.ExecuteReader();
                                if (dr.HasRows == true)
                                {
                                    registeredStudents.Add(student);


                                }

                                dr.Close();
                                cmd.Dispose();

                            }

                        }

                        if (registeredStudents.Count > 0)
                        {
                            // Send to LMS

                            foreach (StudentProfile student in registeredStudents)
                            {

                                LMSProfile profile = GetStudentProfile(student);

                                if (TransmitProfile(profile) == true)

                                { // Update Student Table 

                                    cmd = new SqlCommand($"Update Students set LMSUserId={profile.Id}, [IdTransmitedToLMS]=1 where [StudentId] ={student.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();

                                }

                            }

                        }


                        Thread.Sleep(20000);
                    }
                    else
                    {
                        try
                        {
                            cnn.Open();
                           _log4net.Info("Previous connection expired and was reestablished");

                        }
                        catch (Exception xe)
                        {
                            _log4net.Error(xe.Message);
                        }

                        Thread.Sleep(1000);
                    }




                     
                 _log4net.Info($"Registration Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                    
                    Thread.Sleep(3000);
                }

            }
            catch (Exception xe)
            {
               _log4net.Error(xe.Message);
            }
        }

        private static LMSProfile GetStudentProfile(StudentProfile student)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSSettings");



            LMSProfile profile = new LMSProfile();
            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();
            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();
            profile.wsfunction = configsession.GetSection("ProfileWSfunction").Value.Trim();
            profile.moodlewsrestformat = "json";
            profile.createpassword = int.Parse(configsession.GetSection("CreatePassword").Value.Trim()); // Do not generate random password

            profile.username = student.MatricNumber.Split(new char[] { '/', ' ', '-', '_' }).Aggregate((a, b) => (a + b)).ToLower();

            profile.password = string.Empty;

            if (profile.createpassword == 1)
            {
                profile.password = profile.username.Trim();

            }

            profile.auth = "manual";
            profile.firstname = student.FirstName;
            profile.lastname = student.LastName;
            profile.email = student.Email;
            profile.maildisplay = int.Parse(configsession.GetSection("MailDisplay").Value.Trim());
            profile.idnumber = profile.username;// configsession.GetSection("IdNumber").Value.Trim();
            profile.usersLang = "en";

            return profile;
        }

        private static bool TransmitProfile(LMSProfile profile)

        {
            bool ret = false;

            try
            {
                // 
                var client = new HttpClient();

                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&users[0][createpassword]={profile.createpassword.ToString()}&users[0][username]={profile.username}&users[0][auth]={profile.auth}&users[0][password]={profile.password}&users[0][firstname]={profile.firstname}&users[0][lastname]={profile.lastname}&users[0][email]={profile.email}&users[0][maildisplay]={profile.maildisplay}&users[0][idnumber]={profile.idnumber}&users[0][lang]={profile.usersLang}";

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (ProfileTransmitedFeedBack != null)
                {


                    if (ProfileTransmitedFeedBack.ToString().Contains("exception") != true)
                    {
                        List<UserLMS> usersData = JsonConvert.DeserializeObject<List<UserLMS>>(ProfileTransmitedFeedBack);

                        if (usersData.Count > 0)
                        {
                            profile.Id = usersData[0].Id;
                            ret = true;

                        }
                    }
                    else
                    {
                        _log4net.Warn($"UserId={profile.idnumber} - {ProfileTransmitedFeedBack.ToString()}");
                    }
                }

            }
            catch (Exception ex)
            {

               _log4net.Error(ex.Message);
            }

            return ret;
        }

    }



}