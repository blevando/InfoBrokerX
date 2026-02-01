namespace InfoBroker
{
    using InfoBroker.Models;
    using log4net;
    using log4net.Config;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.OpenApi.Models;
    using Microsoft.VisualBasic;
    using MSMQ.Messaging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using RestSharp; // Nugget Version = 105.2.3 - This is what worked
    using System;
    //using RestSharp.Authenticators;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.Eventing.Reader;
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
    using System.Text.RegularExpressions;
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
            Thread InvoiceProcessor = null;
            Thread PaymentProcessor = null;
            Thread PaymentPosting = null;
            Thread RegisterStudents = null;
            Thread CreateGrading = null;
            //Thread Email = null;
            Thread CreateCourse;
            Thread DeleteCourses;
            Thread UnEnrollment = null;
            Thread SuspendUser = null;
            Thread SudentGrads = null;
            Thread RevenueTracker = null;

            //Thread updateDepartments;

            //Thread updateFaculties;

            //Thread UpdatePrograms;

            Credentials = new Thread(new ParameterizedThreadStart(CredentialsHandler));
            Credentials.Start();
            Interlocked.Increment(ref threadCount);

            UpdatePayment = new Thread(new ParameterizedThreadStart(PaymentUpdateHandler));
            //UpdatePayment.Start();
            Interlocked.Increment(ref threadCount);

            InvoiceProcessor = new Thread(new ParameterizedThreadStart(InvoiceProcessorHandler));
            InvoiceProcessor.Start();
            Interlocked.Increment(ref threadCount);

            PaymentProcessor = new Thread(new ParameterizedThreadStart(PaymentProcessorHandler));
            PaymentProcessor.Start();
            Interlocked.Increment(ref threadCount);

            PaymentPosting = new Thread(new ParameterizedThreadStart(PostPaymentAgainstInvoiceProcessor));
            PaymentPosting.Start();
            Interlocked.Increment(ref threadCount);



            RegisterStudents = new Thread(new ParameterizedThreadStart(CourseRegistrationHandler));
            RegisterStudents.Start();
            Interlocked.Increment(ref threadCount);

            CreateCourse = new Thread(new ParameterizedThreadStart(CreateCourseHandler));
            CreateCourse.Start();
            Interlocked.Increment(ref threadCount);

            CreateGrading = new Thread(new ParameterizedThreadStart(CreateGradingHandler));
            CreateGrading.Start();
            Interlocked.Increment(ref threadCount);

            SudentGrads = new Thread(new ParameterizedThreadStart(GetSudentGradesHandler));
            SudentGrads.Start();
            Interlocked.Increment(ref threadCount);


            DeleteCourses = new Thread(new ParameterizedThreadStart(DeleteCoursesHandler));
            //DeleteCourses.Start();
            Interlocked.Increment(ref threadCount);

            UnEnrollment = new Thread(new ParameterizedThreadStart(UnEnrollmentHandler));
            //UnEnrollment.Start();
            Interlocked.Increment(ref threadCount);

            SuspendUser = new Thread(new ParameterizedThreadStart(SuspendUserHandler));
            //SuspendUser.Start();
            Interlocked.Increment(ref threadCount);



            #region MyRegion

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
            #endregion

            while (!isApplicationProcessing == false)

            {
                _log4net.Info($"Main Tread is running {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");

                Thread.Sleep(3000);
            }

        }



        private static void InvoiceProcessorHandler(object? source)
        {

            // string InvoiceTransactionQueue = $"{@".\Private$\"}{ERPconfigsession.GetSection("InvoiceTransactionQueue").Value.Trim()}";
            string PaymentInstallmentQueue = $"{@".\Private$\"}{System.Configuration.ConfigurationManager.AppSettings["paymentQueue"]}";

            while (!isApplicationProcessing == false)
            {
                try
                {
                    MessageQueue queue;
                    if (!MessageQueue.Exists(PaymentInstallmentQueue))
                    {
                        queue = MessageQueue.Create(PaymentInstallmentQueue);
                        queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(PTrans) });
                        //queue = new MessageQueue(PaymentInstallmentQueue);
                    }
                    else
                    {
                        queue = new MessageQueue(PaymentInstallmentQueue);
                        queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(PTrans) });

                    }

                    _log4net.Info($"Invoice Processor Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");

                    // Probe new payment from the Payment Transaction table
                    string connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString;
                    SqlConnection con = new SqlConnection(connectionstring);
                    con.Open();
                    SqlCommand cmd = null;
                    List<PTrans> paymentInstallments = new List<PTrans>();


                    if (con.State == System.Data.ConnectionState.Open)
                    {
                        // One invice that is not yet transmitted 
                        cmd = new SqlCommand($"Select PT.PaymentTransactionId,PT.PayerId, PT.FullName, PT.ProgrammeId,PT.Email,  PT.Amount, PT.FeeTypeId, PT.PaymentReference, PT.PaymentDescription, PT.PaymentChannel, PT.SessionId, PT.SemesterId, PT.SessionSemester, PT.PaymentDate, FT.FeeTypeCode,FT.BankAccount, PG.ApplicantBPCode, PG.ApplicantAcceptBPCode, PG.StudentBPCode from PaymentTransaction PT Join FeeType FT on PT.FeeTypeId=FT.FeeTypeId Join Programme PG on PT.ProgrammeId=PG.ProgrammeId where PT.isTransmitedToERP = 0", con);
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows == true)
                        {

                            while (reader.Read() == true)
                            {
                                PTrans trans = new PTrans();

                                trans.PaymentTransactionId = reader.IsDBNull(0) ? 0 : long.Parse(reader.GetValue(0).ToString());
                                trans.PayerId = reader.IsDBNull(1) ? "" : reader.GetString(1).ToString(); // The matriculation /application id
                                trans.FullName = reader.IsDBNull(2) ? "" : reader.GetString(2).ToString();
                                trans.ProgrammeId = reader.IsDBNull(3) ? 0 : int.Parse(reader.GetValue(3).ToString());
                                trans.Email = reader.IsDBNull(4) ? "" : reader.GetString(4).ToString();
                                trans.Amount = reader.IsDBNull(5) ? 0 : decimal.Parse(reader.GetValue(5).ToString());
                                trans.FeeTypeId = reader.IsDBNull(6) ? 0 : int.Parse(reader.GetValue(6).ToString());
                                trans.PaymentReference = reader.IsDBNull(7) ? "" : reader.GetString(7).ToString();
                                trans.PaymentDescription = reader.IsDBNull(8) ? "" : reader.GetString(8).ToString();
                                trans.PaymentChannel = reader.IsDBNull(9) ? "" : reader.GetString(9).ToString();
                                trans.SessionId = reader.IsDBNull(10) ? 0 : int.Parse(reader.GetValue(10).ToString());
                                trans.SemesterId = reader.IsDBNull(11) ? 0 : int.Parse(reader.GetValue(11).ToString());
                                trans.SessionSemester = reader.IsDBNull(12) ? "" : reader.GetString(12).ToString();
                                trans.PaymentDate = reader.IsDBNull(13) ? DateTime.MinValue : DateTime.Parse(reader.GetValue(13).ToString());
                                trans.FeeTypeCode = reader.IsDBNull(14) ? "" : reader.GetString(14).ToString();
                                trans.BankAccount = reader.IsDBNull(15) ? "" : reader.GetString(15).ToString();
                                trans.ApplicantBPCode = reader.IsDBNull(16) ? "" : reader.GetString(16).ToString();
                                trans.ApplicantAcceptBPCode = reader.IsDBNull(17) ? "" : reader.GetString(17).ToString();
                                trans.StudentBPCode = reader.IsDBNull(18) ? "" : reader.GetString(18).ToString();

                                paymentInstallments.Add(trans);

                            }

                        }
                        reader.Close();
                        cmd.Dispose();
                    }

                    // Send transactions to Queue for onward processing
                    if (paymentInstallments.Count > 0)
                    {
                        MessageQueue q = new MessageQueue(PaymentInstallmentQueue);
                        q.DefaultPropertiesToSend.Recoverable = true; // Ensure disk storage
                        q.DefaultPropertiesToSend.Priority = MessagePriority.High;

                        foreach (var trans in paymentInstallments)
                        {
                            try
                            {
                                q.Label = $"{trans.PaymentTransactionId}-{trans.FeeTypeId}-{trans.FullName}";
                                q.Send(trans);
                                UpdatePaymentTransaction(trans.PaymentTransactionId, con);

                            }
                            catch (Exception ex)
                            {

                                _log4net.Error($"Error probing Payments {ex.Message}");
                            }
                            Thread.Sleep(5000);
                        }
                        q.Dispose();

                    }
                    else
                    {
                        _log4net.Info("No new Payment to process");
                        Thread.Sleep(10000);
                        continue;
                    }

                }
                catch (Exception ex)
                {

                    _log4net.Error($"Error probing Payments {ex.Message}");
                }
                Thread.Sleep(50000);
            }

        }
        private static void UpdatePaymentTransaction(long paymentTransactionId, SqlConnection con)
        {
            if (con.State == ConnectionState.Open)
            {

                SqlCommand cmd = new SqlCommand($"Update [PaymentTransaction] set [isTransmitedToERP]= 1 where [isTransmitedToERP] = 0", con);
                try
                {
                    cmd.ExecuteNonQuery();
                    _log4net.Info($"PaymentTransactionId {paymentTransactionId} Queued for onward processing");
                }
                catch (Exception ex)
                {

                    _log4net.Error($"Error in Updating PaymentTransaction: {ex.Message}");
                }
            }

        }

        private static void PaymentProcessorHandler(object? obj)
        {
            //Qeueue listener
            string PaymentInstallmentQueue = $"{@".\Private$\"}{System.Configuration.ConfigurationManager.AppSettings["paymentQueue"]}";

            while (!isApplicationProcessing == false)
            {
                try
                {
                    MessageQueue queue = new MessageQueue(@$".\Private$\{PaymentInstallmentQueue}");
                    queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(PTrans) });
                    _log4net.Info($"Payment Processor Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");
                    queue.PeekCompleted += new PeekCompletedEventHandler(InvoicePeekHandler);
                    queue.BeginPeek();


                }
                catch (Exception ex)
                {
                    _log4net.Error($"Error in Processing Payment Transactions: {ex.Message}");
                }
                Thread.Sleep(30000);
            }
        }


        private static void InvoicePeekHandler(object source, PeekCompletedEventArgs e)
        {
            MessageQueue mq = (MessageQueue)source;
            Message m = mq.EndPeek(e.AsyncResult);
            var paymentTrans = (PTrans)m.Body;
            _log4net.Info($"Processing PaymentTransactionId {paymentTrans.PaymentTransactionId} for {paymentTrans.FullName}");


            string? InvoiceNumber = ProcessInvoiceTransaction(paymentTrans);

            _log4net.Info($"PaymentTransactionId {paymentTrans.PaymentTransactionId} processed with Invoice Number {InvoiceNumber}");
            if (InvoiceNumber != null)
            {

                //  PostPaymentAgainstInvoiceTransaction(InvoiceNumber, paymentTrans);

                mq.ReceiveById(m.Id);
            }
            else
            {
                _log4net.Error($"Error in Processing PaymentTransactionId {paymentTrans.PaymentTransactionId} for {paymentTrans.FullName}");
                // Send it back to the queue after some delay
                mq.ReceiveById(m.Id);
                mq.Send(paymentTrans);

                Thread.Sleep(10000);
            }
            //Today


            mq.BeginPeek();
        }

        private static string? ProcessInvoiceTransaction(PTrans paymentTrans)
        {
            string? invoiceNumber = null;
            string connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString;
            SqlConnection con = new SqlConnection(connectionstring);
            con.Open();
            SqlCommand cmd = null;
            movingNum = long.Parse(DateTime.Now.ToString("yyyyMMddHHmm"));
            movingNum = movingNum + 1;

            lnNumber = lnNumber;
            if (lnNumber > 100)
            {
                lnNumber = 1;
            }
            string NumAtCard = $"{paymentTrans.PayerId}-{paymentTrans.FullName.Replace(',', ' ')}";

            if (con.State == System.Data.ConnectionState.Open)
            {
                // One invice that is not yet transmitted 
                cmd = new SqlCommand($"Select [InvoiceNumber] from [InvoiceTransactions] where MatricNumber='{paymentTrans.PayerId}' SessionId={paymentTrans.SessionId} and SemesterId={paymentTrans.SemesterId} and ", con);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows == true)
                {
                    // It exists
                    //This student already has an invoice for this session and semester
                    while (reader.Read() == true)
                    {
                        invoiceNumber = reader.IsDBNull(0) ? "" : reader.GetString(0).ToString();
                    }

                    reader.Close();
                    cmd.Dispose();


                }
                else
                {
                    //This invoice does not exist, create new invoice in ERP System
                    reader.Close();
                    cmd.Dispose();




                    var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();


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

                    //Insert new Invoice Transaction into ERP System and get the Invoice Number
                    //1. Login to ERP System and get the Token
                    //2. Create Invoice Transaction Object
                    //3. Send Invoice Transaction to ERP System
                    //4. Get the Invoice Number from ERP System Response

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

                    if (signinresponse.ResponseStatus == ResponseStatus.Completed)
                    {
                        // Prepare the invoice and send

                        //string someJson = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""NumAtCard"":""" + studentName + @""",""U_PortalInvoiceNo"":""" + invoicenumber + @""",""DocumentLines"": [{""LineNum"": " + 0 + @",""ItemCode"":""" + ItemCode + @""",""Quantity"": " + 1 + @", ""Price"": " + Convert.ToInt32(model.Amount) + @"}]}";

                        string CardCode = "";
                        switch (paymentTrans.FeeTypeCode.Trim().ToUpper())
                        {
                            case application:
                                CardCode = BPCodeApplicant;
                                break;
                            case masters:
                                CardCode = BPCodeMasters;
                                break;
                            case pg:
                                CardCode = BPCodePG;
                                break;
                            default:
                                CardCode = BPCodeMasters;
                                break;
                        }
                        string ItemCode = "";
                        switch (paymentTrans.FeeTypeCode.Trim().ToUpper())
                        {
                            case "JHU-APPL":
                                ItemCode = paymentTrans.ApplicantBPCode;
                                break;
                            case "JHU-ACCP":
                                ItemCode = paymentTrans.ApplicantAcceptBPCode;
                                break;
                            case "JHU-TUIT":
                                ItemCode = paymentTrans.StudentBPCode;
                                break;
                            default:
                                ItemCode = paymentTrans.StudentBPCode;
                                break;
                        }
                        //JHU-APPL
                        //JHU-ACCP
                        //JHU-TUIT

                        // ItemCode = paymentTrans.StudentBPCode;
                        //JHU-MTRC
                        //JHU-LATE
                        //JHU-PGDT
                        //JHU-RETN
                        //JHU-WALT
                        //JHU-PRECRE
                        //JHU-SHORT


                        // Stage 4, prepare invoice and push to erp and get responce
                        //lnNumber = lnNumber;
                        //if (lnNumber > 100)
                        //{
                        //    lnNumber = 1;
                        //}

                        //movingNum = movingNum + 1;
                        // Get the invoice Amount for the payment transaction
                        string requiredAmount = "0";
                        string discount = "0";

                        if (con.State == System.Data.ConnectionState.Open)
                        {
                            // One invice that is not yet transmitted 

                            cmd = new SqlCommand($"Select [RequiredAmount],[Discount] from [Invoice_new] where [MatricNumber]='{paymentTrans.PayerId}' and [Session]={paymentTrans.SessionId} and [Semester]={paymentTrans.SemesterId}", con);

                            reader = cmd.ExecuteReader();
                            if (reader.HasRows == true)
                            {

                                while (reader.Read() == true)
                                {
                                    requiredAmount = reader.IsDBNull(0) ? "0" : reader.GetValue(0).ToString();
                                    discount = reader.IsDBNull(1) ? "0" : reader.GetValue(1).ToString();
                                }
                            }
                        }

                        DocumentLines[] doc = new DocumentLines[1];
                        doc[0] = new DocumentLines
                        {
                            ItemCode = ItemCode,
                            Quantity = 1,
                            Price = Math.Round(double.Parse(requiredAmount), 2),
                            LineNum = lnNumber
                        };



                        // Create the invoice
                        // var docs = Array.Empty<DocumentLines>();

                        // docs.Append(doc);

                        PaymentInvoice invoice = new PaymentInvoice();
                        invoice.NumAtCard = $"{paymentTrans.PayerId}-{paymentTrans.FullName.Replace(',', ' ')}";

                        invoice.DocumentLines = doc;
                        invoice.U_PortalInvoiceNo = $"JHU-{DateTime.Now.ToString("00yy")}-{paymentTrans.PaymentTransactionId}"; //    $"JHU-{movingNum.ToString().Substring(0, 4)}-{movingNum.ToString().Substring(4, movingNum.ToString().Length - 4)}"; ; // to be handled
                        invoice.CardCode = CardCode;

                        _log4net.Info($"Invoice Number: {invoice.U_PortalInvoiceNo} ----- Line Number={doc[0].LineNum} - Payer ID= {paymentTrans.PayerId}");



                        invoice.DocDate = DateTime.Parse(paymentTrans.PaymentDate.ToString()).ToString("yyyy-MM-dd");// HH:mm:ss.fff");

                        // Then serialize it

                        var invoiceData = System.Text.Json.JsonSerializer.Serialize(invoice) + "\n" + @"";  // 

                        _log4net.Info(invoiceData);

                        var B1session = signinresponse.Cookies.Where(a => a.Name == "B1SESSION").Select(a => a.Value).FirstOrDefault();
                        var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();
                        string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


                        //var B1session = "cecf3b76-4dc9-11ec-8000-005056010273";
                        //var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();

                        //string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


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

                        string json = invoiceresponse.Content.ToString();

                        var match = Regex.Match(json, "\"DocEntry\"\\s*:\\s*(\\d+)");
                        int docEntry = 0;
                        if (match.Success)
                        {
                            docEntry = int.Parse(match.Groups[1].Value);

                            invoiceNumber = docEntry.ToString(); // This is the generated Invoice Number


                            reader.Close();
                            cmd.Dispose();
                            //Insert into Invoice Transaction Table
                            cmd = new SqlCommand("Insert into [NumAtCard],[PortalInvoiceNo], [InvoiceTransactions] ([MatricNumber],[LastName],[FirstName],[ProgrammeId],[ProgrammeName], [InvoiceNumber],[FeeType],[InvoiceCode],[CreatedDate],[ModifiedDate],[TotalAmount],[SessionId],[SemesterId],[PaymentStatus]) values (@MatricNumber, @LastName, @FirstName,@ProgrammeId,@ProgrammeId, @InvoiceNumber, @FeeType, @InvoiceCode), @CreatedDate,@ModifiedDate,@TotalAmount,@SessionId,@SemesterId,@PaymentStatus", con);

                            cmd.Parameters.AddWithValue("@MatricNumber", paymentTrans.PayerId);
                            string[] names = paymentTrans.FullName.Split(new char[] { ' ' });
                            cmd.Parameters.AddWithValue("@LastName", names[1]);
                            cmd.Parameters.AddWithValue("@FirstName", names[0]);
                            cmd.Parameters.AddWithValue("@ProgrammeId", paymentTrans.ProgrammeId);
                            cmd.Parameters.AddWithValue("@ProgrammeName", paymentTrans.PaymentTransactionId);

                            cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                            cmd.Parameters.AddWithValue("@FeeType", paymentTrans.FeeTypeCode);
                            cmd.Parameters.AddWithValue("@InvoiceCode", invoice.U_PortalInvoiceNo);
                            cmd.Parameters.AddWithValue("@CreatedDate", paymentTrans.PaymentDate);
                            cmd.Parameters.AddWithValue("@ModifiedDate", paymentTrans.PaymentDate);

                            cmd.Parameters.AddWithValue("@TotalAmount", doc[0].Price);

                            double balance = double.Parse(requiredAmount) - double.Parse(paymentTrans.Amount.ToString());

                            cmd.Parameters.AddWithValue("@Balance", balance);
                            cmd.Parameters.AddWithValue("@SessionId", paymentTrans.SessionId);
                            cmd.Parameters.AddWithValue("@SemesterId", paymentTrans.SemesterId);

                            cmd.Parameters.AddWithValue("@PaymentStatus", 0);

                            cmd.ExecuteNonQuery();
                            cmd.Dispose();


                        }


                    }
                    reader.Close();
                    cmd.Dispose();
                }

                // Post the payment against the invoice number

                cmd = new SqlCommand("Insert into [PaymentInstallment] ([InvoiceNumber],[InstallmentNumber],[Amount],[PaymentDate],[PaymentCode],[PaymentReference],[CustomerName],[BankAccount],[PostStatus]) values (@InvoiceNumber, @InstallmentNumber, @Amount, @PaymentDate,@PaymentCode,@PaymentReference, @CustomerName,@BankAccount, @PostStatus", con);

                // @InvoiceNumber, @InstallmentNumber, @Amount, @PaymentDate,@PaymentCode,@PaymentReference

                cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                cmd.Parameters.AddWithValue("@InstallmentNumber", lnNumber);
                cmd.Parameters.AddWithValue("@Amount", paymentTrans.Amount);
                cmd.Parameters.AddWithValue("@PaymentDate", paymentTrans.PaymentDate);
                cmd.Parameters.AddWithValue("@PaymentCode", paymentTrans.FeeTypeCode); //verify                  
                cmd.Parameters.AddWithValue("@PaymentReference", paymentTrans.PaymentReference);

                cmd.Parameters.AddWithValue("@CustomerName", NumAtCard);
                cmd.Parameters.AddWithValue("@BankAccount", paymentTrans.BankAccount);




                cmd.Parameters.AddWithValue("@PostStatus", 0); // Not yet posted

                cmd.ExecuteNonQuery();
                cmd.Dispose();


            }

            return invoiceNumber;
        }
        private static void PostPaymentAgainstInvoiceProcessor(object source)
        {
            //Post Payment against the generated Invoice Number

            string connectionstring = System.Configuration.ConfigurationManager.ConnectionStrings["connectionstring"].ConnectionString;
            SqlConnection con = new SqlConnection(connectionstring);

            try
            {
                con.Open();
            }
            catch (Exception ex)
            {

                _log4net.Info(ex.Message);
            }
            while (!isApplicationProcessing == false)
            {


                try
                {

                    if (con.State == System.Data.ConnectionState.Open)
                    {
                        // One invice that is not yet transmitted

                        SqlCommand cmd = new SqlCommand($"Select  distinct [InvoiceNumber]  from [PaymentInstallment] where [PostStatus]=0", con);
                        SqlDataReader reader = cmd.ExecuteReader();
                        if (reader.HasRows == true)
                        {
                            List<string> invoiceNumbers = new List<string>();

                            while (reader.Read() == true)
                            {

                                invoiceNumbers.Add(reader.IsDBNull(0) ? "" : reader.GetString(0).ToString());
                            }
                            reader.Close();
                            cmd.Dispose();

                            if (invoiceNumbers.Count > 0)
                            {
                                //Login to ERP System and get the Token
                                Dictionary<long, string> dictionary = new Dictionary<long, string>();
                                foreach (string invoiceNumber in invoiceNumbers)
                                {
                                    cmd = new SqlCommand($"Select [InstallmentNumber], [Amount], [PaymentDate],[PaymentCode],[PaymentReference], [CustomerName],[InvoiceNumber], [Id] from [PaymentInstallment] where [PostStatus]=0", con);
                                    reader = cmd.ExecuteReader();
                                    if (reader.HasRows == true)
                                    {
                                        var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();

                                        var ERPconfigsession = configBuilder.GetSection("ERPSettings");

                                        string authenticationEndPoint = ERPconfigsession.GetSection("AuthRUL").Value.Trim();
                                        //  string invoiceEndPoint = ERPconfigsession.GetSection("InvoiceEndPoint").Value.Trim();
                                        string paymentEndPoint = ERPconfigsession.GetSection("PaymentEndPoint").Value.Trim();

                                        //string BPCodeApplicant = ERPconfigsession.GetSection("BPCodeApplicant").Value.Trim().ToUpper();
                                        //string BPCodeMasters = ERPconfigsession.GetSection("BPCodeMasters").Value.Trim().ToUpper();
                                        //string BPCodePG = ERPconfigsession.GetSection("BPCodePG").Value.Trim().ToUpper();
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

                                        if (signinresponse.ResponseStatus == ResponseStatus.Completed)
                                        {

                                            while (reader.Read() == true)
                                            {
                                                PaymentInstallment paymentTrans = new PaymentInstallment();

                                                paymentTrans.InstallmentNumber = reader.IsDBNull(0) ? 0 : int.Parse(reader.GetValue(0).ToString());
                                                paymentTrans.Amount = reader.IsDBNull(1) ? 0 : decimal.Parse(reader.GetValue(1).ToString());
                                                paymentTrans.PaymentDate = reader.IsDBNull(2) ? DateTime.MinValue : DateTime.Parse(reader.GetValue(2).ToString());
                                                paymentTrans.PaymentCode = reader.IsDBNull(3) ? "" : reader.GetString(3).ToString();
                                                paymentTrans.PaymentReference = reader.IsDBNull(4) ? "" : reader.GetString(4).ToString();
                                                paymentTrans.CustomerName = reader.IsDBNull(5) ? "" : reader.GetString(5).ToString();
                                                paymentTrans.PortalReceiptNo = reader.IsDBNull(6) ? "" : reader.GetString(6).ToString();
                                                paymentTrans.InvoiceNumber = reader.IsDBNull(7) ? "" : reader.GetString(7).ToString();
                                                // Post Payment against Invoice Number
                                                long imtId = reader.IsDBNull(8) ? 0 : long.Parse(reader.GetValue(8).ToString());
                                                reader.Close();
                                                cmd.Dispose();

                                                //  cmd = new SqlCommand($"Select [NumAtCard],[CardCode],[U_PortalInvoiceNo] from [Invoices] where [DocEntry]={invoiceNumber}", con);




                                                PaymentReceived received = new PaymentReceived();

                                                PaymentInvoices individualInvoice = new PaymentInvoices();

                                                individualInvoice.SumApplied = double.Parse(paymentTrans.Amount.ToString());
                                                //individualInvoice.DocEntry = payment.FeeTypeId.ToString();

                                                individualInvoice.DocEntry = invoiceNumber;
                                                individualInvoice.InvoiceType = paymentTrans.PaymentCode;

                                                individualInvoice.LineNumber = paymentTrans.InstallmentNumber.ToString();

                                                // Now we add in
                                                received.PaymentInvoices = individualInvoice;

                                                received.U_CustName = paymentTrans.CustomerName;
                                                received.CardCode = paymentTrans.PaymentCode;
                                                received.TransferAccount = paymentTrans.BankAccount;
                                                received.TransferSum = individualInvoice.SumApplied.ToString();
                                                received.DocDate = DateTime.Parse(paymentTrans.PaymentDate.ToString()).ToString("yyyy-MM-dd");
                                                received.U_PortalReceiptNo = paymentTrans.PortalReceiptNo.Replace("JHU", "RPT");


                                                // We convert the received to JSON

                                                var paymentData = System.Text.Json.JsonSerializer.Serialize(received) + "\n" + @"";

                                                _log4net.Info(paymentData);

                                                var B1session = signinresponse.Cookies.Where(a => a.Name == "B1SESSION").Select(a => a.Value).FirstOrDefault();
                                                var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();
                                                string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


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
                                                    // Posting payment against invoice was successful
                                                    //We can not update the installment payment and the invoice transaction itself itself

                                                    //Payment first
                                                    dictionary.Add(imtId, invoiceNumber);


                                                }


                                            }
                                        }
                                    }



                                }

                                // hh
                                cmd.Dispose();
                                if (dictionary.Count > 0)
                                {
                                    foreach (var item in dictionary)
                                    {
                                        cmd = new SqlCommand($"update [PaymentInstallment] set [PostStatus] = 1 where [Id] = {item.Key} and [InvoiceNumber] = '{item.Value}'", con);
                                        cmd.ExecuteNonQuery();
                                        cmd.Dispose(); 
                                        
                                        
                                        cmd = new SqlCommand("Select sum(TotalAmount) from [PaymentInstallment] where [InvoiceNumber]= '{item.Value}' ", con);
                                        //ToDo

                                    }
                                }
                            }


                        }

                    }
                    else
                    {
                        con.Dispose();
                        Thread.Sleep(1000);
                        try
                        {
                            con.Open();
                        }
                        catch (Exception ex)
                        {

                            _log4net.Info(ex.Message);
                        }

                    }


                }
                catch (Exception ex)
                {

                    _log4net.Info(ex.Message);
                }

            }
        }

        private static void SuspendUserHandler(object? obj)
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
                        //SqlCommand cmd = new SqlCommand(@"select Id, LMSUserId, LMSCourseId from Coursedeletion where HasLMSDeleted=0", cnn);
                        SqlCommand cmd = new SqlCommand(@"Select StudentId, LMSUserId from Students where studentstatusId >1 and LMSUserID >0 and isSuspended=0", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();
                        List<Suspendmodel> suspendUsers = new List<Suspendmodel>();
                        if (dr.HasRows == true)
                        {
                            var configBuilder1 = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
                            var configsession1 = configBuilder1.GetSection("LMSSuspendUser");
                            //string YearID = configsession1.GetSection("CurrentYearId").Value.Trim();//.ToInteger();
                            //string SemesterId = configsession1.GetSection("CurrentSemesterId").Value.Trim();
                            Suspendmodel suspUser = null;
                            while (dr.Read() == true)
                            {
                                suspUser = new Suspendmodel();
                                suspUser.StudentId = int.Parse(dr.GetValue(0).ToString());
                                suspUser.LMSUserId = int.Parse(dr.GetValue(1).ToString()).ToString();//.Split(new char[] { '/', ' ', '-', '_' }).Aggregate((a, b) => (a + b)).ToLower();
                                suspendUsers.Add(suspUser);
                            }
                        }

                        dr.Close();
                        cmd.Dispose();
                        if (suspendUsers.Count > 0)
                        {
                            // Send to LMS                      

                            foreach (Suspendmodel suspUser in suspendUsers)
                            {
                                // Send to LMS
                                SuspendUserProfile profile = GetSuspendUser(suspUser, cnn);

                                if (SuspendedStudent(profile) == true)

                                { // Update Student Table 

                                    cmd = new SqlCommand($"Update [Students] set [isSuspended]=1 where [LMSUserId] ={suspUser.LMSUserId}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();
                                    _log4net.Info($"UserID {profile.userid} successfully suspended");
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

        private static void UnEnrollmentHandler(object? obj)
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
                        //SqlCommand cmd = new SqlCommand(@"select Id, LMSUserId, LMSCourseId from Coursedeletion where HasLMSDeleted=0 and LmsCourseId !=null ", cnn);
                        SqlCommand cmd = new SqlCommand(@"select Id, LMSUserId, LMSCourseId from Coursedeletion where LmsCourseId is not NULL and HasLMSDeleted=0", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();

                        List<CourseUnEnrolled> registrations = new List<CourseUnEnrolled>();
                        if (dr.HasRows == true)
                        {
                            var configBuilder1 = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
                            var configsession1 = configBuilder1.GetSection("LMSUnEnrolled");
                            string YearID = configsession1.GetSection("CurrentYearId").Value.Trim();//.ToInteger();
                            string SemesterId = configsession1.GetSection("CurrentSemesterId").Value.Trim();
                            CourseUnEnrolled registration = null;
                            while (dr.Read() == true)
                            {
                                registration = new CourseUnEnrolled();
                                registration.Id = int.Parse(dr.GetValue(0).ToString());
                                registration.LMSUserId = int.Parse(dr.GetValue(1).ToString()).ToString(); //.Split(new char[] { '/', ' ', '-', '_' }).Aggregate((a, b) => (a + b)).ToLower();
                                registration.LMSCourseId = int.Parse(dr.GetValue(2).ToString()).ToString();
                                registrations.Add(registration);
                            }
                        }

                        dr.Close();
                        cmd.Dispose();
                        if (registrations.Count > 0)
                        {
                            // Send to LMS                      

                            foreach (CourseUnEnrolled registration in registrations)
                            {
                                // Send to LMS
                                CourseUnEnrolledProfile profile = GetCourseUnEnrolled(registration, cnn);

                                if (RegisteredStudent(profile) == true)

                                { // Update Student Table 

                                    cmd = new SqlCommand($"Update [coursedeletion] set [HasLMSDeleted]=1 where [Id] ={registration.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();
                                    _log4net.Info($"UserID {profile.userid} successfully UnEnrolled in Course Id={profile.courseid}");
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
        private static bool SuspendedStudent(SuspendUserProfile profile)
        {
            bool ret = false;
            try
            {
                // 
                var client = new HttpClient();
                // string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&enrolments[0][roleid]={profile.roleid}&enrolments[0][userid]={profile.userid}&enrolments[0][courseid]={profile.courseid}";
                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&users[0][id]={profile.userid}&users[0][suspended]={profile.Suspend}";

                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                string aa = ProfileTransmitedFeedBack.ToString().Replace("\"warnings\":[]", "--");
                if (aa == "{--}")
                {
                    if (ProfileTransmitedFeedBack.ToString().Contains("exception") != true)
                    {
                        _log4net.Info($"UserId {profile.userid} is Suspended Successfully");
                        ret = true;
                    }
                    else
                    {
                        ret = false;
                        _log4net.Warn($"Course {profile.userid} - {ProfileTransmitedFeedBack.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {

                _log4net.Error(ex.Message);
            }
            return ret;
        }
        private static bool RegisteredStudent(CourseUnEnrolledProfile profile)
        {
            bool ret = false;

            try
            {
                // 
                var client = new HttpClient();
                // string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&enrolments[0][roleid]={profile.roleid}&enrolments[0][userid]={profile.userid}&enrolments[0][courseid]={profile.courseid}";
                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&enrolments[0][userid]={profile.userid}&enrolments[0][courseid]={profile.courseid}";

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
                            _log4net.Info($"Course {profile.courseid} Unenrollment for UserID {profile.userid} - {profile.username} - {ProfileTransmitedFeedBack.ToString()}");
                            ret = true;
                        }
                    }
                    else
                    {
                        _log4net.Warn($"UserID {profile.userid} - {ProfileTransmitedFeedBack.ToString()}");
                    }

                }

            }
            catch (Exception ex)
            {

                _log4net.Error(ex.Message);
            }
            return ret;
        }

        private static CourseUnEnrolledProfile GetCourseUnEnrolled(CourseUnEnrolled registration, SqlConnection cnn)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSUnEnrolled");
            CourseUnEnrolledProfile profile = new CourseUnEnrolledProfile();
            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();
            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();
            profile.wsfunction = configsession.GetSection("ProfileWSfunction").Value.Trim();
            profile.roleid = configsession.GetSection("RoleId").Value.Trim();
            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();
            profile.userid = registration.LMSUserId;
            profile.courseid = registration.LMSCourseId;
            return profile;
        }
        private static SuspendUserProfile GetSuspendUser(Suspendmodel suUser, SqlConnection cnn)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSSuspendUser");
            SuspendUserProfile profile = new SuspendUserProfile();
            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();
            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();
            profile.wsfunction = configsession.GetSection("WSfunction").Value.Trim();
            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();
            profile.userid = suUser.LMSUserId;
            profile.Suspend = configsession.GetSection("Suspend").Value.Trim();
            return profile;
        }

        private static void DeleteCoursesHandler(object? obj)
        {
            _log4net.Info("Course Registration Module fired");
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            SqlConnection cnn = new SqlConnection(connectionstring);
            try
            {
                cnn.Open();

                while (!isApplicationProcessing == false)
                {
                    if (cnn.State == ConnectionState.Open)
                    {
                        SqlCommand cmd = new SqlCommand("Select CourseCode,LmsCourseId as CourseId from [EduCourseSchedule] where [IsDeleted]=1", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();

                        if (dr.HasRows == true)
                        {
                            List<DeleteCoursesDto> coursesToDelete = new List<DeleteCoursesDto>();
                            while (dr.Read() == true)
                            {
                                DeleteCoursesDto user = new DeleteCoursesDto();
                                user.CourseCode = dr.GetString(0).ToString();
                                user.CourseId = int.Parse(dr.GetValue(1).ToString());
                                coursesToDelete.Add(user);

                            }

                            dr.Close();
                            cmd.Dispose();
                            if (coursesToDelete.Count > 0)
                            {

                                foreach (DeleteCoursesDto course in coursesToDelete)
                                {

                                    GetDeleteCoursesProfile profile = GetProfileOfCoursesToDelete(course);

                                    bool isDeleted = DeleteCourse(profile);

                                    if (isDeleted == true)
                                    {

                                        {
                                            cmd = new SqlCommand($"Update [EduCourseSchedule] set IsDeleted = 2 where [LMSCourseId]='{course.CourseId}'", cnn);
                                            cmd.ExecuteNonQuery();
                                            cmd.Dispose();
                                            _log4net.Info($"The Course Id {profile.courseid} is Deleted Successfully");
                                        }

                                    }
                                }

                            }
                        }
                        dr.Close();
                        cmd.Dispose();
                    }
                    else
                    {
                        cnn.Close();
                        cnn.Open();
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (Exception ex)
            {
                _log4net.Error(ex.Message);
            }


        }

        private static bool DeleteCourse(GetDeleteCoursesProfile profile)
        {
            bool ret = false;
            try
            {
                HttpClient client = new HttpClient();
                //GradesResponse gradesResponse = new GradesResponse();

                // string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&userid={profile.UserId}";
                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&courseids[0]={profile.courseid}";



                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // string aa = ProfileTransmitedFeedBack.ToString().Replace("null","--");
                //string aa = ProfileTransmitedFeedBack.ToString().Replace("null", "--");
                string aa = ProfileTransmitedFeedBack.ToString().Replace("\"warnings\":[]", "--");
                //if (ProfileTransmitedFeedBack.ToString() == "[]")
                if (aa == "{--}")
                {
                    if (ProfileTransmitedFeedBack.ToString().Contains("exception") != true)
                    {
                        //var usersData = JsonConvert.DeserializeObject<List<UserLMS>>(ProfileTransmitedFeedBack);
                        //if (usersData == null)
                        //{
                        _log4net.Info($"CourseId {profile.courseid} is Deleted Successfully");
                        ret = true;
                        // }
                    }
                    else
                    {
                        ret = false;
                        _log4net.Warn($"Course {profile.courseid} - {ProfileTransmitedFeedBack.ToString()}");
                    }

                }
            }
            catch (Exception xe)
            {
                _log4net.Error(xe.Message);
            }
            return ret;
        }

        private static GetDeleteCoursesProfile GetProfileOfCoursesToDelete(DeleteCoursesDto course)
        {
            GetDeleteCoursesProfile profile = new GetDeleteCoursesProfile();

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSDeleteCourses");

            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();

            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

            profile.wsfunction = configsession.GetSection("WSfunction").Value.Trim();

            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();

            profile.courseid = course.CourseId.Value;


            return profile;
        }

        private static void GetSudentGradesHandler(object? obj)
        {
            // string? a =  obj.ToString();
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");
            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();
            SqlConnection cnn = new SqlConnection(connectionstring);

            try
            {
                cnn.Open();

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {
                        //SqlCommand cmd = new SqlCommand("select MatricNumber, LMSUserId from Students where MatricNumber in (  Select distinct MatricNumber from CourseRegistration where score is null and SessionId =15 and SchoolSemesterId =1)", cnn);  //E.IdTransmitedToLMS=1\r\n", cnn);
                        SqlCommand cmd = new SqlCommand($"INSERT INTO CRG (MatricNumber, LMSCourseId,LMSUserId,Score,RawGrade) SELECT R.MatricNumber, R.LMSCourseId, MIN(S.LMSUserId) AS LMSUserId, 0 AS Score, 0 AS RawGrade FROM CourseRegistration R JOIN Students S ON R.MatricNumber = S.MatricNumber WHERE   R.LMSCourseId is not null AND CONCAT(R.MatricNumber, R.LMSCourseId) NOT IN ( SELECT CONCAT(MatricNumber, LMSCourseId) FROM CRG ) GROUP BY R.MatricNumber, R.LMSCourseId order by R.MatricNumber, R.LMSCourseId", cnn);
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();

                        cmd = new SqlCommand($"UPDATE C SET C.Score = COALESCE(R.Score, R.Score),  C.RawGrade = COALESCE(R.RawScore, R.RawScore) FROM CRG C INNER JOIN CourseRegistration R  ON C.MatricNumber = R.MatricNumber AND C.LMSCourseId = R.LMSCourseId WHERE R.Score IS NOT NULL  OR R.RawScore IS NOT NULL", cnn);
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();

                        //cmd = new SqlCommand("select MatricNumber, LMSUserId from Students where MatricNumber in (  Select distinct MatricNumber from CourseRegistration)", cnn);  //E.IdTransmitedToLMS=1\r\n", cnn);
                        cmd = new SqlCommand("select MatricNumber,  LMSUserId, LMSCourseId from CRG", cnn);

                        SqlDataReader dr = cmd.ExecuteReader();

                        if (dr.HasRows == true)
                        {
                            List<EnrolledUserDTO> enrolledUsers = new List<EnrolledUserDTO>();

                            while (dr.Read() == true)
                            {
                                EnrolledUserDTO user = new EnrolledUserDTO();

                                user.MatricNumber = dr.GetString(0).ToString();

                                user.UserId = int.Parse(dr.GetValue(1).ToString());

                                user.CourseId = int.Parse(dr.GetValue(2).ToString());

                                enrolledUsers.Add(user);

                            }

                            dr.Close();
                            cmd.Dispose();


                            if (enrolledUsers.Count > 0)
                            {
                                GetRawGradeProfile profile = GetGradeProfile();

                                foreach (EnrolledUserDTO enrolleduser in enrolledUsers)
                                {

                                    //Create a profile 
                                    //Call LMS to get rawgrades
                                    // serriallize it, 
                                    // Update the examenrollments table
                                    //if ( enrolleduser.CourseId >= 90 || enrolleduser.CourseId <= 98)
                                    //if ( enrolleduser.CourseId >= 0 || enrolleduser.CourseId >= 98)
                                    // {
                                    //     _log4net.Warn($"CourseId {enrolleduser.CourseId} is not valid for {enrolleduser.MatricNumber}");
                                    //     continue;
                                    // }

                                    profile.UserId = enrolleduser.UserId.ToString();

                                    List<GradeDetail> gradeDetails = new List<GradeDetail>();

                                    gradeDetails = FetchGrade(profile);

                                    if (gradeDetails != null)
                                    {
                                        foreach (GradeDetail gradeDetail in gradeDetails)
                                        {
                                            //if (enrolleduser.UserId == 52)
                                            {
                                                switch (gradeDetail.Grade)
                                                {


                                                    case "-":
                                                        break; // do nothing there is no grade rawgrade is also null at this point

                                                    default:
                                                        // Pick from LMS and Upgrade CourseRegistration
                                                        // since CourseRegistraion is already updating and inserting into CRG
                                                        dr.Close();
                                                        cmd = new SqlCommand($"Update CourseRegistration set [Score] = {gradeDetail.Grade},[RawScore]={gradeDetail.RawGrade} where [MatricNumber] ={enrolleduser.MatricNumber} and LMSCourseId={gradeDetail.CourseId}", cnn);
                                                        cmd.ExecuteNonQuery();
                                                        cmd.Dispose();
                                                        break;

                                                }
                                            }

                                        }


                                    }

                                }
                            }


                        }

                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try
                        {
                            cnn.Dispose();
                            cnn.Close();
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

                    Thread.Sleep(2000);
                }

            }
            catch (Exception xe)
            {
                _log4net.Error(xe.Message);
            }
        }
        private static void CreateGradingHandler(object? obj)
        {
            // string? a =  obj.ToString();
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");
            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();
            SqlConnection cnn = new SqlConnection(connectionstring);

            try
            {
                cnn.Open();

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {
                        // first update the table
                        // SqlCommand cmd = new SqlCommand("select E.Id,  c.provisionedid as courseId, E.provisionedid as userid from ExamCourses c, ExamEnrollments E where e.examCode = c.examCode and E.ActiveStatus=0 and e.isProvisioned=1", cnn);
                        //SqlCommand cmd = new SqlCommand("select E.MatricNumber,  c.LMSCourseId as courseId, E.LMSUserID as userid from EduCourseSchedule c, Students E where E.IdTransmitedToLMS = c.isTransmitedToLMS and c.isTransmitedToLMS=1 and E.IdTransmitedToLMS=1", cnn);
                        //string cdon = "select E.MatricNumber,  c.LMSCourseId as courseId, E.LMSUserID as userid from EduCourseSchedule c inner join CourseRegistration r on c.LMSCourseId = r.LMSCourseId inner join Students e on e.MatricNumber = r.MatricNumber where e.IdTransmitedToLMS = c.isTransmitedToLMSand c.isTransmitedToLMS = 1 and e.IdTransmitedToLMS = 1";
                        //SqlCommand cmd = new SqlCommand("select E.MatricNumber,  c.LMSCourseId as courseId, E.LMSUserID as userid from EduCourseSchedule c inner join CourseRegistration R  on c.LMSCourseId=r.LMSCourseId inner join Students E on e.MatricNumber=r.MatricNumber  where E.IdTransmitedToLMS = c.isTransmitedToLMS  and c.isTransmitedToLMS=1 and E.IdTransmitedToLMS=1", cnn);  //E.IdTransmitedToLMS=1\r\n", cnn);
                        SqlCommand cmd = new SqlCommand("select E.MatricNumber,  c.LMSCourseId as courseId, E.LMSUserID as userid from EduCourseSchedule c inner join CourseRegistration R  on c.LMSCourseId=r.LMSCourseId inner join Students E on e.MatricNumber=r.MatricNumber order by c.LMSCourseId", cnn);  //E.IdTransmitedToLMS=1\r\n", cnn);


                        SqlDataReader dr = cmd.ExecuteReader();


                        if (dr.HasRows == true)
                        {
                            List<EnrolledUserDTO> enrolledUsers = new List<EnrolledUserDTO>();

                            while (dr.Read() == true)
                            {
                                EnrolledUserDTO user = new EnrolledUserDTO();
                                //user.MatricNumber = int.Parse(dr.GetValue(0).ToString());
                                user.MatricNumber = (string)dr.GetValue(0);
                                user.CourseId = int.Parse(dr.GetValue(1).ToString());
                                user.UserId = int.Parse(dr.GetValue(2).ToString());

                                enrolledUsers.Add(user);

                            }

                            dr.Close();
                            cmd.Dispose();


                            if (enrolledUsers.Count > 0)
                            {
                                GetRawGradeProfile profile = GetGradeProfile();

                                foreach (EnrolledUserDTO enrolleduser in enrolledUsers)
                                {

                                    //Create a profile 
                                    //Call LMS to get rawgrades
                                    // serriallize it, 
                                    // Update the examenrollments table
                                    _log4net.Warn($"CourseId {enrolleduser.CourseId} is not valid for {enrolleduser.MatricNumber}");

                                    //if (enrolleduser.CourseId >= 90 && enrolleduser.CourseId <= 98)
                                    //{
                                    //    _log4net.Warn($"CourseId {enrolleduser.CourseId} is not valid for {enrolleduser.MatricNumber}");
                                    //}


                                    profile.UserId = enrolleduser.UserId.ToString();

                                    List<GradeDetail> gradeDetails = new List<GradeDetail>();

                                    gradeDetails = FetchGrade(profile);

                                    if (gradeDetails != null)
                                    {
                                        foreach (GradeDetail gradeDetail in gradeDetails)
                                        {
                                            //if (enrolleduser.UserId == 84)

                                            {
                                                switch (gradeDetail.Grade)
                                                {


                                                    case "-":
                                                        break; // do nothing there is no grade rawgrade is also null at this point

                                                    default:

                                                        //if (enrolleduser.CourseId == gradeDetail.CourseId && enrolleduser.UserId.ToString() == profile.UserId)
                                                        //{
                                                        dr.DisposeAsync();
                                                        // cmd = new SqlCommand($"Update ExamEnrollments set [Score] = {gradeDetail.Grade},[RawScore]={gradeDetail.RawGrade},[HasScored]= {1},[IsResultPicked]={0}  where [Id] ={enrolleduser.Id}", cnn);
                                                        cmd = new SqlCommand($"Update CourseRegistration set [Score] = {gradeDetail.Grade},[RawScore]={gradeDetail.RawGrade} where  [MatricNumber] ='{enrolleduser.MatricNumber}' and [LMSCourseId]={gradeDetail.CourseId}", cnn);
                                                        cmd.ExecuteNonQuery();
                                                        cmd.Dispose();
                                                        _log4net.Info($"Scores of {enrolleduser.MatricNumber}{profile.UserId} are: Grade: {gradeDetail.Grade}, RawGrade={gradeDetail.RawGrade}");
                                                        // }

                                                        break;

                                                }
                                            }

                                        }


                                    }

                                }
                            }


                        }

                        Thread.Sleep(2000);
                    }
                    else
                    {
                        try
                        {
                            cnn.Dispose();
                            cnn.Close();
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

                    Thread.Sleep(2000);
                }

            }
            catch (Exception xe)
            {
                _log4net.Error(xe.Message);
            }
        }

        private static List<GradeDetail> FetchGrade(GetRawGradeProfile profile)
        {
            HttpClient client = new HttpClient();
            GradesResponse gradesResponse = new GradesResponse();
            string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&userid={profile.UserId}";


            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            var response = client.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            string aa = ProfileTransmitedFeedBack.ToString().Replace("null", "--");
            //if (ProfileTransmitedFeedBack.ToString() == "[]")
            //{

            try
            {

                gradesResponse = JsonConvert.DeserializeObject<GradesResponse>(ProfileTransmitedFeedBack);


            }
            catch (Exception xe)
            {
                _log4net.Error(xe.Message);
            }

            return gradesResponse.Grades.Where(o => o.Grade != "-").ToList();
        }

        private static GetRawGradeProfile GetGradeProfile()
        {
            GetRawGradeProfile profile = new GetRawGradeProfile();

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("LMSGetGrade");


            profile.wstoken = configsession.GetSection("LMSToken").Value.Trim();

            profile.LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

            profile.wsfunction = configsession.GetSection("WSfunction").Value.Trim();

            profile.moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();


            return profile;
        }

        private static void CreateCourseHandler(object? obj)
        {
            _log4net.Info("Course Registration Module fired");

            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            var lmsregistration = configBuilder.GetSection("LMSRegistration");
            string currentSemester = lmsregistration.GetSection("CurrentSemesterId").Value.Trim();
            string yearid = lmsregistration.GetSection("CurrentYearId").Value.Trim();
            SqlConnection cnn = new SqlConnection(connectionstring);
            cnn.Open();

            while (!isApplicationProcessing == false)
            {
                try
                {
                    if (cnn.State == ConnectionState.Open)
                    {
                        SqlCommand cmd = new SqlCommand($"Select [srn],[CourseCode], [CourseTitle],[YearId],[SemesterId] from [EduCourseSchedule] where [YearId]='{yearid}' and SemesterId={currentSemester} and [isTransmitedToLMS]=0 and [isDeleted]=0", cnn);
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
                                schedule.CourseTitle = dr.GetString(2).ToString().Replace("&", "and");
                                schedule.YearId = dr.GetString(3);
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
                                //string catId =   CreateCategory(profile.CategoryId,$"{schedule.YearId} Semester {schedule.SemesterId}");
                                string catId = CreateCategory(profile.CategoryId, $"{schedule.YearId}");

                                profile.CategoryId = catId;
                                int LMSid = CreateCourse(profile);

                                if (LMSid > 0)

                                {

                                    // Update Student Table 
                                    cmd = new SqlCommand($"Update EduCourseSchedule set [LMSCourseId] = {LMSid}, [isTransmitedToLMS]=1 where [srn] ={schedule.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();

                                    // cmd = new SqlCommand($"Update CourseRegistration set [LMSCourseId] = {LMSid}, [isRegisteredToLMS]=1 where [CourseRegistrationId] ={schedule.Id}", cnn);
                                    cmd = new SqlCommand($"Update CourseRegistration set [LMSCourseId] = {LMSid} where [CourseCode] ='{schedule.CourseCode}'", cnn);
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

                    _log4net.Info($"Create Course Broker firing {DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff")}");

                    Thread.Sleep(2000);
                }
                catch (Exception xe)
                {
                    _log4net.Error(xe.Message);
                }
            }



        }

        private static int CreateCourse(CourseScheduleProfile profile)
        {

            int LMSid = 0;

            // $"{registration.CourseCode.Trim().ToUpper()}_{registration.SessionId.ToString()}_{registration.SchoolSemesterId.ToString()}";

            var client = new HttpClient();

            //https://class.jhu.edu.ng/webservice/rest/server.php?wstoken=•••••••&wsfunction=core_course_create_courses&moodlewsrestformat=json&courses[0][fullname]=Software Architecting&courses[0][shortname]=CSC154&courses[0][categoryid]=2&courses[0][idnumber]=CSC154&courses[0][summary]=CSC154 transitions students to programming on the UNIX machines. The class aims to teach students about computer systems from the hardware up to the source code. Topics include machine architecture (registers, I/O, basic assembly language), memory models (pointers, memory allocation, data representation), compilation (stack frames, semantic analysis, code generation), and basic concurrency (threading, synchronization).&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]=1689202800&courses[0][enddate]=1702422000&courses[0][visible]=1
            //wsfunction=core_course_create_courses&moodlewsrestformat=json&courses[0][fullname]=Software Architecting&courses[0][shortname]=CSC154&courses[0][categoryid]=2&courses[0][idnumber]=CSC154&courses[0][summary]=CSC154 transitions students to programming on the UNIX machines. The class aims to teach students about computer systems from the hardware up to the source code. Topics include machine architecture (registers, I/O, basic assembly language), memory models (pointers, memory allocation, data representation), compilation (stack frames, semantic analysis, code generation), and basic concurrency (threading, synchronization).&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]=1689202800&courses[0][enddate]=1702422000&courses[0][visible]=1

            //Check is course already exists

            string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&courses[0][fullname]={@profile.CourseTitle}&courses[0][shortname]={profile.ShortName}&courses[0][categoryid]={profile.CategoryId}&courses[0][idnumber]={profile.courseid}&courses[0][summary]={profile.CourseDescription}&courses[0][summaryformat]=1&courses[0][format]=topics&courses[0][showgrades]=1&courses[0][newsitems]=5&courses[0][startdate]={profile.StartDate}&courses[0][enddate]={profile.EndDate}&courses[0][visible]=1";

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            var response = client.SendAsync(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            string aa = ProfileTransmitedFeedBack.ToString();

            List<CreateCourseResponse> usersData = new List<CreateCourseResponse>();
            // _log4net.Info(aa);
            try
            {

                if (aa.Contains("exception") == false)
                {
                    usersData = JsonConvert.DeserializeObject<List<CreateCourseResponse>>(ProfileTransmitedFeedBack);
                }
                else
                {
                    _log4net.Warn(aa);
                }

            }
            catch (Exception ex)
            {

                _log4net.Error($"Create Course Error: {ex.Message}");
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
            //profile.CategoryId  = $"{schedule.YearId.LastFourCharacters()}{schedule.SemesterId}";      
            profile.CategoryId = $"{schedule.YearId.LastFourCharacters()}";

            //profile.ShortName = $@"{schedule.CourseCode.ToUpper()}";
            //profile.courseid = $@"{schedule.CourseCode.ToUpper()}";// {schedule.YearId.LastTwoCharacters()}{schedule.SemesterId}";//schedule.CourseCode.ToUpper();

            profile.ShortName = $@"{schedule.CourseCode.ToUpper()}_{schedule.Id}";
            profile.courseid = $@"{schedule.CourseCode.ToUpper()}{schedule.Id}";// {schedule.YearId.LastTwoCharacters()}{schedule.SemesterId}";//schedule.CourseCode.ToUpper();

            profile.CourseTitle = $"{schedule.CourseCode}-{schedule.CourseTitle}";
            //profile.CourseTitle = $"{schedule.CourseTitle}";
            profile.CourseDescription = $@"This course is {schedule.CourseTitle} ...";


            return profile;

        }

        private static string CreateCategory(string categoryId, string categoryName)
        {

            string catid = string.Empty;
            try
            {
                var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
                var configsession = configBuilder.GetSection("LMSCreateCourse");

                string wstoken = configsession.GetSection("LMSToken").Value.Trim();

                string LMSUrl = configsession.GetSection("LMSUrl").Value.Trim();

                string wsfunction = configsession.GetSection("WSfunction").Value.Trim();

                string moodlewsrestformat = configsession.GetSection("Moodlewsrestformat").Value.Trim();
                string rootcategoryid = configsession.GetSection("RootCategoryId").Value.Trim();


                var client = new HttpClient();

                string apiUrl = $@"https://class.jhu.edu.ng/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_course_get_categories&moodlewsrestformat=json&criteria[0][key]=idnumber&criteria[0][value]={categoryId}";


                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                var response = client.SendAsync(request).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                string aa = ProfileTransmitedFeedBack.ToString();
                if (ProfileTransmitedFeedBack.ToString() == "[]")
                {
                    //  Itdoes notexists  - Createit
                    client.Dispose();
                    client = new HttpClient();


                    apiUrl = $@"https://class.jhu.edu.ng/webservice/rest/server.php?wstoken={wstoken}&wsfunction=core_course_create_categories&moodlewsrestformat=json&categories[0][name]={categoryName}&categories[0][parent]={rootcategoryid}&categories[0][idnumber]={categoryId}";
                    request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                    response = client.SendAsync(request).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    ProfileTransmitedFeedBack = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    aa = ProfileTransmitedFeedBack.ToString();

                    //catid

                    List<CreateCategoryResponse> cat = JsonConvert.DeserializeObject<List<CreateCategoryResponse>>(ProfileTransmitedFeedBack);
                    foreach (CreateCategoryResponse itm in cat)
                    {

                        catid = itm.Id.ToString();
                    }
                }
                else
                {
                    List<CreateCourseCategoryResponse> usersData = JsonConvert.DeserializeObject<List<CreateCourseCategoryResponse>>(ProfileTransmitedFeedBack);
                    //foreach (CreateCourseCategoryResponse itms in usersData)
                    //{

                    //    catid = itms.Id.ToString();
                    //    break;
                    //}
                    if (usersData.Count > 0)
                    {
                        catid = usersData[0].Id.ToString();
                    }
                    // cat id
                }



                _log4net.Info(aa);

            }
            catch (Exception ex)
            {

                _log4net.Error($"Create Course Error: {ex.Message}");
            }

            return catid;

        }

        private static void CourseRegistrationHandler(object? obj)
        {
            var configBuilder = new ConfigurationBuilder().AddJsonFile("Settings.json").Build();
            var configsession = configBuilder.GetSection("ConnectionString");

            string connectionstring = configsession.GetSection("connectionstring").Value.Trim();

            var LmsSetting = configBuilder.GetSection("LMSRegistration");
            string currentyear = LmsSetting.GetSection("CurrentYearId").Value.Trim();
            string currentsemester = LmsSetting.GetSection("CurrentSemesterId").Value.Trim();

            SqlConnection cnn = new SqlConnection(connectionstring);
            cnn.Open();

            try
            {

                while (!isApplicationProcessing == false)

                {
                    if (cnn.State == ConnectionState.Open)
                    {

                        SqlCommand cmd = new SqlCommand($"Select CR.CourseRegistrationId,CR.MatricNumber,CR.CourseCode,CR.SessionId,CR.SchoolSemesterId,ST.LMSUserId from CourseRegistration CR join Students ST  on CR.MatricNumber = ST.MatricNumber where CR.isRegisteredToLMS=0 and CR.SessionSemester like '{currentyear}%' and CR.schoolsemesterId={currentsemester}", cnn);

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

                                CourseRegistrationProfile profile = GetCourseRegistrationProfile(registration, cnn);

                                //string catId = CreateCategory(profile.CategoryId, $"{DateTime.Now.Year.ToString()}");
                                //profile.CategoryId = catId; //Parent Folder
                                //int LMSid = CreateCourse(profile);

                                if (RegisterStudent(profile) == true)
                                { // Update Student Table 

                                    cmd = new SqlCommand($"Update CourseRegistration set [isRegisteredToLMS]=1, LMSCourseId ={profile.courseid} where [CourseRegistrationId] ={registration.Id}", cnn);
                                    cmd.ExecuteNonQuery();
                                    cmd.Dispose();
                                    _log4net.Info($"UserID {profile.userid} successfully registered in Course Id={profile.courseid}");
                                }

                                //Also Registered Students in the General Courses

                                RegisterStudentInGeneralCourses(profile, registration);

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

        private static void RegisterStudentInGeneralCourses(CourseRegistrationProfile profile, CourseRegistration registration)
        {
            try
            {

                string courseid = profile.courseid;
                courseid = "105"; // Hard coded becauses the courses were created manually
                                  // 
                var client = new HttpClient();
                string apiUrl = $@"{profile.LMSUrl}/webservice/rest/server.php?wstoken={profile.wstoken}&wsfunction={profile.wsfunction}&moodlewsrestformat={profile.moodlewsrestformat}&enrolments[0][roleid]={profile.roleid}&enrolments[0][userid]={profile.userid}&enrolments[0][courseid]={courseid}";

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

                            _log4net.Info($"UserID {profile.userid} - {profile.username} - {ProfileTransmitedFeedBack.ToString()}");




                        }
                    }
                    else
                    {
                        _log4net.Warn($"Student Orientation: UserID {profile.userid} - {profile.username} - {ProfileTransmitedFeedBack.ToString()}");
                    }

                }

            }
            catch (Exception ex)
            {

                _log4net.Error(ex.Message);
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

                            _log4net.Info($"UserID {profile.userid} - {profile.username} - {ProfileTransmitedFeedBack.ToString()}");

                            ret = true;


                        }
                    }
                    else
                    {
                        _log4net.Warn($"UserID {profile.userid} in {profile.courseid} - {ProfileTransmitedFeedBack.ToString()}");
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

            SqlCommand cmd = new SqlCommand($@"select LMSCourseId from EduCourseSchedule where CourseCode = '{coursecode}' and SemesterId={SemesterId} and isTransmitedtolms=1 and YearId = '{YearId}'", cnn);
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.HasRows == true)
            {
                if (dr.Read() == true)
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
            string InvoiceTransactionQueue = $"{@".\Private$\"}{ERPconfigsession.GetSection("InvoiceTransactionQueue").Value.Trim()}";
            //string PaymentInstallmentQueue = $"{@".\Private$\"}{ERPconfigsession.GetSection("PaymentInstallmentQueue").Value.Trim()}";




            // BP Codes
            const string application = "JHU-APPL";
            const string masters = "JHU-TUIT";
            const string pg = "JHU-PGDT";

            try
            {

                //
                SqlConnection cnn = new SqlConnection(connectionstring);
                cnn.Open();
                while (!isApplicationProcessing == false)
                {
                    //

                    if (cnn.State == ConnectionState.Open)
                    {

                        // Stage 1 - Are there paid transactions
                        SqlCommand cmd = new SqlCommand($"Select PT.PaymentTransactionId,PT.PayerId, PT.FullName, PT.ProgrammeId,PT.Email,  PT.Amount, PT.FeeTypeId, PT.PaymentReference, PT.PaymentDescription, PT.PaymentChannel, PT.SessionId, PT.SemesterId, PT.SessionSemester, PT.PaymentDate, FT.FeeTypeCode,FT.BankAccount, PG.ApplicantBPCode, PG.ApplicantAcceptBPCode, PG.StudentBPCode from PaymentTransaction PT Join FeeType FT on PT.FeeTypeId=FT.FeeTypeId Join Programme PG on PT.ProgrammeId=PG.ProgrammeId where PT.isTransmitedToERP = 0", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();

                        if (dr.HasRows)
                        {
                            List<PaymentProfile> Paids = new List<PaymentProfile>();

                            try
                            {
                                while (dr.Read())
                                {

                                    PaymentProfile Paid = new PaymentProfile(); // Create a new instance in every loop iteration

                                    if (!dr.IsDBNull(0))
                                        Paid.Id = int.Parse(dr.GetValue(0).ToString());
                                    if (!dr.IsDBNull(1))
                                        Paid.PayerId = dr.GetString(1);
                                    if (!dr.IsDBNull(2))
                                        Paid.FullName = dr.GetString(2);

                                    Paid.ProgrammeId = int.Parse(dr.GetValue(3).ToString());
                                    Paid.Email = dr.GetString(4);
                                    Paid.Amount = dr.GetValue(5).ToString();

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

                                    switch (Paid.FeeTypeCode.Trim().ToUpper())
                                    {
                                        case "JHU-APPL":
                                            Paid.ItemCode = dr.GetString(16).Trim().ToUpper();
                                            break;
                                        case "JHU-ACCP":
                                            Paid.ItemCode = dr.GetString(17).Trim().ToUpper();
                                            break;
                                        case "JHU-TUIT":
                                            Paid.ItemCode = dr.GetString(18).Trim().ToUpper();
                                            break;
                                        default:
                                            Paid.ItemCode = dr.GetString(18).Trim().ToUpper();
                                            break;
                                    }

                                    Paids.Add(Paid); // Add the newly created object to the list
                                }
                            }
                            catch (Exception ex)
                            {
                                _log4net.Error($"Error reading payment transactions: {ex.Message}");
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

                                if (signinresponse.ResponseStatus == ResponseStatus.Completed)
                                {
                                    // Prepare the invoice and send

                                    //string someJson = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""NumAtCard"":""" + studentName + @""",""U_PortalInvoiceNo"":""" + invoicenumber + @""",""DocumentLines"": [{""LineNum"": " + 0 + @",""ItemCode"":""" + ItemCode + @""",""Quantity"": " + 1 + @", ""Price"": " + Convert.ToInt32(model.Amount) + @"}]}";


                                    foreach (PaymentProfile payment in Paids)
                                    {

                                        // Stage 4, prepare invoice and push to erp and get responce
                                        lnNumber = lnNumber;
                                        if (lnNumber > 100)
                                        {
                                            lnNumber = 1;
                                        }

                                        movingNum = movingNum + 1;


                                        DocumentLines[] doc = new DocumentLines[1];
                                        doc[0] = new DocumentLines
                                        {
                                            ItemCode = payment.ItemCode,
                                            Quantity = 1,
                                            Price = double.Parse(payment.Amount),
                                            LineNum = lnNumber
                                        };
                                        //doc[0].Price = double.Parse(payment.Amount);
                                        //doc[0].Quantity = 1;
                                        //doc[0].LineNum = lnNumber;
                                        //doc[0].ItemCode = payment.ItemCode;

                                        // Switch Structure is hidden in the rejion bracket
                                        #region


                                        #endregion

                                        // Create the invoice
                                        // var docs = Array.Empty<DocumentLines>();

                                        // docs.Append(doc);

                                        PaymentInvoice invoice = new PaymentInvoice();
                                        invoice.NumAtCard = payment.FullName.Replace(',', ' ');

                                        invoice.DocumentLines = doc;
                                        invoice.U_PortalInvoiceNo = $"JHU-{DateTime.Now.ToString("00yy")}-{payment.Id}"; //    $"JHU-{movingNum.ToString().Substring(0, 4)}-{movingNum.ToString().Substring(4, movingNum.ToString().Length - 4)}"; ; // to be handled
                                        invoice.CardCode = payment.CardCode;

                                        _log4net.Info($"Invoice Number: {invoice.U_PortalInvoiceNo} ----- Line Number={doc[0].LineNum} - Payer ID= {payment.PayerId}");



                                        invoice.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");// HH:mm:ss.fff");

                                        // Then serialize it

                                        var invoiceData = System.Text.Json.JsonSerializer.Serialize(invoice) + "\n" + @"";  // 

                                        _log4net.Info(invoiceData);

                                        var B1session = signinresponse.Cookies.Where(a => a.Name == "B1SESSION").Select(a => a.Value).FirstOrDefault();
                                        var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();
                                        string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


                                        //var B1session = "cecf3b76-4dc9-11ec-8000-005056010273";
                                        //var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();

                                        //string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


                                        var invoiceclient = new RestClient(invoiceEndPoint);
                                        invoiceclient.Timeout = -1;
                                        var invoicerequest = new RestRequest(Method.POST); // It is a post request 

                                        invoicerequest.AddHeader("Content-Type", "application/json");
                                        //invoicerequest.AddHeader("Cookie", "B1SESSION=cecf3b76-4dc9-11ec-8000-005056010273; ROUTEID=.node8");
                                        invoicerequest.AddHeader("Cookie", "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString());
                                        invoicerequest.AddCookie("B1SESSION", B1session.ToString());
                                        invoicerequest.AddCookie("ROUTEID", RouteID.ToString());

                                        invoicerequest.AddParameter("application/json", invoiceData, ParameterType.RequestBody);
                                        int bb = 2;

                                        IRestResponse invoiceresponse = invoiceclient.Execute(invoicerequest);

                                        int aa = 2;
                                        _log4net.Info(invoiceresponse.Content);

                                        string json = invoiceresponse.Content.ToString();

                                        var match = Regex.Match(json, "\"DocEntry\"\\s*:\\s*(\\d+)");
                                        int docEntry = 0;
                                        if (match.Success)
                                        {
                                            docEntry = int.Parse(match.Groups[1].Value);
                                            //Console.WriteLine(docEntry); // 277
                                        }


                                        if (invoiceresponse.ResponseStatus == ResponseStatus.Completed)
                                        {
                                            // This means that the invoice was created successfully, then send the Payment - 
                                            // string p = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""U_CustName"":""" + studentName + @""",""U_PortalReceiptNo"":""" + paymentnumber + @""",""TransferAccount"" :""" + "123301" + @""",""TransferSum"" : " + Convert.ToInt32(model.Amount) + @",""PaymentInvoices"": [{""LineNum"": " + 0 + @",""InvoiceType"":""" + "it_Invoice" + @""",""DocEntry"": " + docEntry + @", ""SumApplied"": " + Convert.ToInt32(model.Amount) + @"}]}";

                                            PaymentReceived received = new PaymentReceived();

                                            PaymentInvoices individualInvoice = new PaymentInvoices();

                                            individualInvoice.SumApplied = invoice.DocumentLines[0].Price;
                                            //individualInvoice.DocEntry = payment.FeeTypeId.ToString();

                                            individualInvoice.DocEntry = docEntry.ToString();        //payment.FeeTypeId.ToString();
                                            individualInvoice.InvoiceType = payment.FeeTypeCode;
                                            individualInvoice.SumApplied = double.Parse(payment.Amount);
                                            individualInvoice.LineNumber = invoice.DocumentLines[0].LineNum.ToString();

                                            // Now we add in
                                            received.PaymentInvoices = individualInvoice;

                                            received.U_CustName = invoice.NumAtCard;
                                            received.CardCode = invoice.CardCode;
                                            received.TransferAccount = payment.BankAccount;
                                            received.TransferSum = payment.Amount;
                                            received.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");
                                            received.U_PortalReceiptNo = invoice.U_PortalInvoiceNo.Replace("JHU", "RPT");


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

                                                //cmd = new SqlCommand($"Update PaymentTransaction set isTransmitedToERP=1 where PaymentTransactionId ={payment.Id}", cnn);
                                                cmd = new SqlCommand($"Update PaymentTransaction set isTransmitedToERP=1 where PaymentTransactionId ={payment.Id}", cnn);
                                                cmd.ExecuteNonQuery();
                                                cmd.Dispose();

                                                // Update the database : Payment Transaction Table, set isTransmittedToERP = 1 for the payment.Id field

                                            }
                                            else
                                            {
                                                Thread.Sleep(12000);
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

                                Thread.Sleep(2000);
                                _log4net.Info("The database reconnected");

                            }

                        }

                    }


                    // Try end

                }
            }
            catch (Exception ex)
            {
                _log4net.Error(ex.Message);
                Thread.Sleep(12000);
            }

        }




        private static void OldPaymentUpdateHandler(object? source)
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

                //
                SqlConnection cnn = new SqlConnection(connectionstring);
                cnn.Open();
                while (!isApplicationProcessing == false)
                {
                    //
                    if (cnn.State == ConnectionState.Open)
                    {

                        // Stage 1 - Are there pain transactions
                        SqlCommand cmd = new SqlCommand($"Select PT.PaymentTransactionId,PT.PayerId, PT.FullName, PT.ProgrammeId,PT.Email,  PT.Amount, PT.FeeTypeId, PT.PaymentReference, PT.PaymentDescription, PT.PaymentChannel, PT.SessionId, PT.SemesterId, PT.SessionSemester, PT.PaymentDate, FT.FeeTypeCode,FT.BankAccount, PG.ApplicantBPCode, PG.ApplicantAcceptBPCode, PG.StudentBPCode from PaymentTransaction PT Join FeeType FT on PT.FeeTypeId=FT.FeeTypeId Join Programme PG on PT.ProgrammeId=PG.ProgrammeId where PT.isTransmitedToERP = 0", cnn);
                        SqlDataReader dr = cmd.ExecuteReader();

                        if (dr.HasRows)
                        {
                            List<PaymentProfile> Paids = new List<PaymentProfile>();

                            try
                            {
                                while (dr.Read())
                                {

                                    PaymentProfile Paid = new PaymentProfile(); // Create a new instance in every loop iteration

                                    if (!dr.IsDBNull(0))
                                        Paid.Id = int.Parse(dr.GetValue(0).ToString());
                                    if (!dr.IsDBNull(1))
                                        Paid.PayerId = dr.GetString(1);
                                    if (!dr.IsDBNull(2))
                                        Paid.FullName = dr.GetString(2);

                                    Paid.ProgrammeId = int.Parse(dr.GetValue(3).ToString());
                                    Paid.Email = dr.GetString(4);
                                    Paid.Amount = dr.GetValue(5).ToString();

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

                                    switch (Paid.FeeTypeCode.Trim().ToUpper())
                                    {
                                        case "JHU-APPL":
                                            Paid.ItemCode = dr.GetString(16).Trim().ToUpper();
                                            break;
                                        case "JHU-ACCP":
                                            Paid.ItemCode = dr.GetString(17).Trim().ToUpper();
                                            break;
                                        case "JHU-TUIT":
                                            Paid.ItemCode = dr.GetString(18).Trim().ToUpper();
                                            break;
                                        default:
                                            Paid.ItemCode = dr.GetString(18).Trim().ToUpper();
                                            break;
                                    }

                                    Paids.Add(Paid); // Add the newly created object to the list
                                }
                            }
                            catch (Exception ex)
                            {
                                _log4net.Error($"Error reading payment transactions: {ex.Message}");
                            }




                            //if (dr.Read())
                            //{
                            //    List<PaymentProfile> Paids = new List<PaymentProfile>();

                            //    if (dr.HasRows == true)
                            //    {
                            //        PaymentProfile Paid = new PaymentProfile();
                            //        //err
                            //        try
                            //        {
                            //            while (dr.Read() == true)
                            //            {
                            //                if (!dr.IsDBNull(0))
                            //                {
                            //                    Paid.Id = int.Parse(dr.GetValue(0).ToString());
                            //                }
                            //                if (!dr.IsDBNull(1))
                            //                {
                            //                    Paid.PayerId = dr.GetString(1);
                            //                }
                            //                if (!dr.IsDBNull(2))
                            //                {
                            //                    Paid.FullName = dr.GetString(2);
                            //                }
                            //                //Paid.Id = int.Parse(dr.GetValue(0).ToString());
                            //                //Paid.PayerId = dr.GetString(1);
                            //                //Paid.PaymentTransactionId = int.Parse(dr.GetValue(0).ToString());
                            //                Paid.ProgrammeId = int.Parse(dr.GetValue(3).ToString());
                            //                Paid.Email = dr.GetString(4); // It is not needed
                            //                Paid.Amount = dr.GetValue(5).ToString();
                            //                Paid.FeeTypeId = int.Parse(dr.GetValue(6).ToString());
                            //                Paid.PaymentReference = dr.GetString(7);
                            //                Paid.PaymentDescriptipon = dr.GetString(8);
                            //                Paid.PaymentChannel = dr.GetString(9);
                            //                Paid.SessionId = int.Parse(dr.GetValue(10).ToString());
                            //                Paid.SemesterId = int.Parse(dr.GetValue(11).ToString());
                            //                Paid.SessionSemester = dr.GetString(12);
                            //                Paid.PaymentDate = dr.GetDateTime(13).ToString("yyyy-MM-dd HH:mm:ss.fff");
                            //                Paid.FeeTypeCode = dr.GetString(14);
                            //                Paid.BankAccount = dr.GetString(15);
                            //                Paid.ItemCode = dr.GetString(16);

                            //                switch (Paid.FeeTypeCode.Trim().ToUpper())
                            //                {

                            //                    case application:
                            //                        Paid.CardCode = BPCodeApplicant;
                            //                        break;

                            //                    case masters:
                            //                        Paid.CardCode = BPCodeMasters;
                            //                        break;

                            //                    case pg:
                            //                        Paid.CardCode = BPCodePG;
                            //                        break;

                            //                    default:
                            //                        Paid.CardCode = BPCodeMasters;
                            //                        break;
                            //                }



                            //                Paids.Add(Paid);
                            //            }
                            //        }
                            //        catch (Exception ex)
                            //        {

                            //            throw;
                            //        }
                            //end err



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

                                if (signinresponse.ResponseStatus == ResponseStatus.Completed)
                                {
                                    // Prepare the invoice and send

                                    //string someJson = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""NumAtCard"":""" + studentName + @""",""U_PortalInvoiceNo"":""" + invoicenumber + @""",""DocumentLines"": [{""LineNum"": " + 0 + @",""ItemCode"":""" + ItemCode + @""",""Quantity"": " + 1 + @", ""Price"": " + Convert.ToInt32(model.Amount) + @"}]}";


                                    foreach (PaymentProfile payment in Paids)
                                    {

                                        // Stage 4, prepare invoice and push to erp and get responce
                                        lnNumber = lnNumber;
                                        if (lnNumber > 100)
                                        {
                                            lnNumber = 1;
                                        }

                                        movingNum = movingNum + 1;


                                        DocumentLines[] doc = new DocumentLines[1];
                                        doc[0] = new DocumentLines
                                        {
                                            ItemCode = payment.ItemCode,
                                            Quantity = 1,
                                            Price = double.Parse(payment.Amount),
                                            LineNum = lnNumber
                                        };
                                        //doc[0].Price = double.Parse(payment.Amount);
                                        //doc[0].Quantity = 1;
                                        //doc[0].LineNum = lnNumber;
                                        //doc[0].ItemCode = payment.ItemCode;

                                        // Switch Structure is hidden in the rejion bracket
                                        #region


                                        #endregion

                                        // Create the invoice
                                        // var docs = Array.Empty<DocumentLines>();

                                        // docs.Append(doc);

                                        PaymentInvoice invoice = new PaymentInvoice();
                                        invoice.NumAtCard = payment.FullName.Replace(',', ' ');

                                        invoice.DocumentLines = doc;
                                        invoice.U_PortalInvoiceNo = $"JHU-{DateTime.Now.ToString("00yy")}-{payment.Id}"; //    $"JHU-{movingNum.ToString().Substring(0, 4)}-{movingNum.ToString().Substring(4, movingNum.ToString().Length - 4)}"; ; // to be handled
                                        invoice.CardCode = payment.CardCode;

                                        _log4net.Info($"Invoice Number: {invoice.U_PortalInvoiceNo} ----- Line Number={doc[0].LineNum} - Payer ID= {payment.PayerId}");



                                        invoice.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");// HH:mm:ss.fff");

                                        // Then serialize it

                                        var invoiceData = System.Text.Json.JsonSerializer.Serialize(invoice) + "\n" + @"";  // 

                                        _log4net.Info(invoiceData);

                                        var B1session = signinresponse.Cookies.Where(a => a.Name == "B1SESSION").Select(a => a.Value).FirstOrDefault();
                                        var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();
                                        string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


                                        //var B1session = "cecf3b76-4dc9-11ec-8000-005056010273";
                                        //var RouteID = signinresponse.Cookies.Where(a => a.Name == "ROUTEID").Select(a => a.Value).FirstOrDefault();

                                        //string cookie = "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString();


                                        var invoiceclient = new RestClient(invoiceEndPoint);
                                        invoiceclient.Timeout = -1;
                                        var invoicerequest = new RestRequest(Method.POST); // It is a post request 

                                        invoicerequest.AddHeader("Content-Type", "application/json");
                                        //invoicerequest.AddHeader("Cookie", "B1SESSION=cecf3b76-4dc9-11ec-8000-005056010273; ROUTEID=.node8");
                                        invoicerequest.AddHeader("Cookie", "B1SESSION=" + B1session.ToString() + "; ROUTEID=" + RouteID.ToString());
                                        invoicerequest.AddCookie("B1SESSION", B1session.ToString());
                                        invoicerequest.AddCookie("ROUTEID", RouteID.ToString());

                                        invoicerequest.AddParameter("application/json", invoiceData, ParameterType.RequestBody);
                                        int bb = 2;

                                        IRestResponse invoiceresponse = invoiceclient.Execute(invoicerequest);

                                        int aa = 2;
                                        _log4net.Info(invoiceresponse.Content);

                                        string json = invoiceresponse.Content.ToString();

                                        var match = Regex.Match(json, "\"DocEntry\"\\s*:\\s*(\\d+)");
                                        int docEntry = 0;
                                        if (match.Success)
                                        {
                                            docEntry = int.Parse(match.Groups[1].Value);
                                            //Console.WriteLine(docEntry); // 277
                                        }


                                        if (invoiceresponse.ResponseStatus == ResponseStatus.Completed)
                                        {
                                            // This means that the invoice was created successfully, then send the Payment - 
                                            // string p = @"{ ""CardCode"":""" + BPCode + @""",""DocDate"":""" + docDate + @""",""U_CustName"":""" + studentName + @""",""U_PortalReceiptNo"":""" + paymentnumber + @""",""TransferAccount"" :""" + "123301" + @""",""TransferSum"" : " + Convert.ToInt32(model.Amount) + @",""PaymentInvoices"": [{""LineNum"": " + 0 + @",""InvoiceType"":""" + "it_Invoice" + @""",""DocEntry"": " + docEntry + @", ""SumApplied"": " + Convert.ToInt32(model.Amount) + @"}]}";

                                            PaymentReceived received = new PaymentReceived();

                                            PaymentInvoices individualInvoice = new PaymentInvoices();

                                            individualInvoice.SumApplied = invoice.DocumentLines[0].Price;
                                            //individualInvoice.DocEntry = payment.FeeTypeId.ToString();

                                            individualInvoice.DocEntry = docEntry.ToString();        //payment.FeeTypeId.ToString();
                                            individualInvoice.InvoiceType = payment.FeeTypeCode;
                                            individualInvoice.SumApplied = double.Parse(payment.Amount);
                                            individualInvoice.LineNumber = invoice.DocumentLines[0].LineNum.ToString();

                                            // Now we add in
                                            received.PaymentInvoices = individualInvoice;

                                            received.U_CustName = invoice.NumAtCard;
                                            received.CardCode = invoice.CardCode;
                                            received.TransferAccount = payment.BankAccount;
                                            received.TransferSum = payment.Amount;
                                            received.DocDate = DateTime.Parse(payment.PaymentDate).ToString("yyyy-MM-dd");
                                            received.U_PortalReceiptNo = invoice.U_PortalInvoiceNo.Replace("JHU", "RPT");


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

                                                //cmd = new SqlCommand($"Update PaymentTransaction set isTransmitedToERP=1 where PaymentTransactionId ={payment.Id}", cnn);
                                                cmd = new SqlCommand($"Update PaymentTransaction set isTransmitedToERP=1 where PaymentTransactionId ={payment.Id}", cnn);
                                                cmd.ExecuteNonQuery();
                                                cmd.Dispose();

                                                // Update the database : Payment Transaction Table, set isTransmittedToERP = 1 for the payment.Id field

                                            }
                                            else
                                            {
                                                Thread.Sleep(12000);
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

                                Thread.Sleep(2000);
                                _log4net.Info("The database reconnected");

                            }

                        }

                    }


                    // Try end

                }
            }
            catch (Exception ex)
            {
                _log4net.Error(ex.Message);
                Thread.Sleep(12000);
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