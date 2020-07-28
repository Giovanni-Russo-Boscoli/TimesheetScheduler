using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimesheetScheduler.Models
{
    public class EmailSender
    {

        //[HttpPost]
        //public void SendEmail()
        //{
        //    //GmailEmailService gmail = new GmailEmailService();
        //    //EmailMessage msg = new EmailMessage();
        //    //msg.Body = "Mensagemmmmmmm";
        //    //msg.IsHtml = true;
        //    //msg.Subject = "Cadastro Realizado";
        //    //msg.ToEmail = "boscoli.giovanni@email.com";
        //    //gmail.SendEmailMessage(msg);
        //    //return gmail.SendEmailMessage(msg);
        //    try
        //    {
        //        var smptClient = new SmtpClient("smtp.gmail.com", 587)
        //        {
        //            Credentials = new NetworkCredential("boscoli.giovanni@gmail.com", "giovanni*1245"),
        //            EnableSsl = true,
        //            UseDefaultCredentials = false
        //        };

        //        smptClient.Send("boscoli.giovanni@gmail.com", "boscoli.giovanni@gmail.com", "Testing Email", "testing the email");
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        //public bool SendEmailMessage(EmailMessage message)
        //{
        //    var success = false;
        //    try
        //    {
        //        var smtp = new SmtpClient
        //        {
        //            Host = _config.Host,
        //            Port = _config.Port,
        //            EnableSsl = _config.Ssl,
        //            DeliveryMethod = SmtpDeliveryMethod.Network,
        //            UseDefaultCredentials = false,
        //            Credentials = new NetworkCredential(_config.Username, _config.Password)
        //        };
        //        using (var smtpMessage = new MailMessage(_config.Username, message.ToEmail))
        //        {
        //            smtpMessage.Subject = message.Subject;
        //            smtpMessage.Body = message.Body;
        //            smtpMessage.IsBodyHtml = message.IsHtml;
        //            smtp.Send(smtpMessage);
        //        }
        //        success = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        //todo: add logging integration
        //        //throw;
        //    }
        //    return success;
        //}

        //[HttpPost]
        //public async void SendEmail(string from, string email, string subject, string comments)
        //{
        //    MailMessage mail = new MailMessage();

        //    mail.From = new MailAddress("boscoli.giovanni@gmail.com");
        //    mail.To.Add("boscoli.giovanni@gmail.com"); // para
        //    mail.Subject = "Teste"; // assunto
        //    mail.Body = "Testando mensagem de e-mail"; // mensagem

        //    // em caso de anexos
        //    //mail.Attachments.Add(new System.Net.Mail.Attachment(@"\\welfare.irlgov.ie\shares\FRDIRWIN7_FI\giovanniboscoli\Desktop\signalR.txt"));//\\welfare.irlgov.ie\shares\FRDIRWIN7_FI\giovanniboscoli\Desktop\signalR.txt

        //    using (var smtp = new SmtpClient("smtp.gmail.com"))
        //    {
        //        smtp.EnableSsl = true; // GMail requer SSL
        //        smtp.Port = 587;       // porta para SSL
        //        smtp.DeliveryMethod = SmtpDeliveryMethod.Network; // modo de envio
        //        smtp.UseDefaultCredentials = false; // vamos utilizar credencias especificas

        //        // seu usuário e senha para autenticação
        //        smtp.Credentials = new NetworkCredential("boscoli.giovanni@gmail.com", "giovanni*1245");

        //        // envia o e-mail
        //        await smtp.SendMailAsync(mail);
        //    }

        //var fromAddress = new MailAddress("boscoli.giovanni@gmail.com", "From Name");
        //var toAddress = new MailAddress("boscoli.giovanni@gmail.com", "To Name");
        //const string fromPassword = "giovanni*1245";
        ////const string _subject = "Subject";
        //string body = comments; // "Body";

        //var smtp = new SmtpClient
        //{
        //    Host = "smtp.gmail.com",
        //    Port = 587,
        //    EnableSsl = true,
        //    DeliveryMethod = SmtpDeliveryMethod.Network,
        //    UseDefaultCredentials = false,
        //    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
        //};
        //using (var message = new MailMessage(fromAddress, toAddress)
        //{
        //    Subject = subject,
        //    Body = body
        //})
        //{
        //    await smtp.SendMailAsync(message);
        //}

        //var sent = false;
        //using (var smtp = new SmtpClient())
        //{
        //    var credential = new NetworkCredential
        //    {
        //        UserName = "boscoli.giovanni@gmail.com",  // replace with valid value
        //        Password = "giovanni*1245"  // replace with valid value
        //    };
        //    smtp.Credentials = credential;
        //    smtp.Host = "smtp-gmail.com"; // "smtp-mail.outlook.com";
        //    smtp.Port = 587;
        //    smtp.EnableSsl = true;
        //    var message = new MailMessage
        //    {
        //        Body = $"From: {from} at {email}<p>{comments}</p>",
        //        Subject = subject,
        //        IsBodyHtml = true
        //    };
        //    message.To.Add("boscoli.giovanni@gmail.com");
        //    await smtp.SendMailAsync(message);
        //    //sent = true;
        //}
        //return sent;
        //}
    }

    //public interface IEmailService
    //{
    //    bool SendEmailMessage(EmailMessage message);
    //}
    //public class SmtpConfiguration
    //{
    //    public string Username { get; set; }
    //    public string Password { get; set; }
    //    public string Host { get; set; }
    //    public int Port { get; set; }
    //    public bool Ssl { get; set; }
    //}
    //public class EmailMessage
    //{
    //    public string ToEmail { get; set; }
    //    public string Subject { get; set; }
    //    public string Body { get; set; }
    //    public bool IsHtml { get; set; }
    //}
    //public class GmailEmailService : IEmailService
    //{
    //    private readonly SmtpConfiguration _config;
    //    public GmailEmailService()
    //    {
    //        _config = new SmtpConfiguration();
    //        var gmailUserName = "boscoli.giovanni@gmail.com";
    //        var gmailPassword = "giovanni*1245";
    //        var gmailHost = "smtp.gmail.com";
    //        var gmailPort = 587;
    //        var gmailSsl = true;
    //        _config.Username = gmailUserName;
    //        _config.Password = gmailPassword;
    //        _config.Host = gmailHost;
    //        _config.Port = gmailPort;
    //        _config.Ssl = gmailSsl;
    //    }
    //    public bool SendEmailMessage(EmailMessage message)
    //    {
    //        var success = false;
    //        try
    //        {
    //            var smtp = new SmtpClient
    //            {
    //                Host = _config.Host,
    //                Port = _config.Port,
    //                EnableSsl = _config.Ssl,
    //                DeliveryMethod = SmtpDeliveryMethod.Network,
    //                UseDefaultCredentials = false,
    //                Credentials = new NetworkCredential(_config.Username, _config.Password)
    //            };
    //            using (var smtpMessage = new MailMessage(_config.Username, message.ToEmail))
    //            {
    //                smtpMessage.Subject = message.Subject;
    //                smtpMessage.Body = message.Body;
    //                smtpMessage.IsBodyHtml = message.IsHtml;
    //                smtp.Send(smtpMessage);
    //            }
    //            success = true;
    //        }
    //        catch (Exception ex)
    //        {
    //            //todo: add logging integration
    //            throw ex;
    //        }
    //        return success;
    //    }
    //}
}