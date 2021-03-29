using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace TimesheetScheduler.Controllers
{
    public class EmailSenderController : Controller
    {
        // GET: EmailSender
        public ActionResult Index()
        {
            return View();
        }

        public string sendEmail()
        {
            try
            {
                //MailMessage mail = new MailMessage();
                //SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
                //SmtpServer.UseDefaultCredentials = false;
                //mail.From = new MailAddress("boscoli.giovanni@gmail.com");
                //mail.To.Add("boscoli.giovanni@gmail.com");
                //mail.Subject = "Test Mail";
                //mail.Body = "This is for testing SMTP mail from GMAIL";

                //SmtpServer.Port = 587;
                //SmtpServer.Credentials = new System.Net.NetworkCredential("boscoli.giovanni@gmail.com", "w60j5ne5");
                //SmtpServer.EnableSsl = true;

                //SmtpServer.Send(mail);
                //Console.Write("mail Send");
                //return "Email sent!";

                string _sender = "giovanni.boscoli@welfare.ie";
                string _password = "?bCh+*p#d8MQ24";

                SmtpClient client = new SmtpClient("smtp-mail.outlook.com");

                client.Port = 587;
                client.DeliveryMethod = SmtpDeliveryMethod.Network;
                client.UseDefaultCredentials = false;
                System.Net.NetworkCredential credentials =
                    new System.Net.NetworkCredential(_sender, _password);
                client.EnableSsl = true;
                client.Credentials = credentials;

                MailMessage message = new MailMessage(_sender, "giovanni.boscoli@welfare.ie");
                message.Subject = "teste email";
                message.Body = "test email body";
                client.Send(message);
                return "Email sent!";
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                return "Error trying to send email!";
            }

        }
    }
}