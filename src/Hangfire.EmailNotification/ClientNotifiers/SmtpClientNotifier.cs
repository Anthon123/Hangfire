using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Email;

namespace Hangfire.EmailNotification
{
    public class SmtpClientNotifier : INotifier
    {
        private SmtpClient _smtpClient;
        Dictionary<EventTypes.Events, List<string>> _eventReceivers = new Dictionary<EventTypes.Events, List<string>>();
        private string _fromEmail;
        private string _fromName;
        
        public SmtpClientNotifier(string fromEmail, string fromName, SmtpConfiguration sendgridConfig)
        {
            _fromEmail = fromEmail;
            _fromName = fromName;
            _smtpClient = new SmtpClient(sendgridConfig.Host, sendgridConfig.Port);
            _smtpClient.Credentials = new System.Net.NetworkCredential(sendgridConfig.Username, sendgridConfig.Password);
        }

        public void Notify(EventTypes.Events eventType, string subject, string message)
        {
            MailMessage mailMsg = new MailMessage();

            List<string> emails;
            _eventReceivers.TryGetValue(eventType,out emails);

            if (emails.Count > 0)
            {
                foreach (string email in emails)
                {
                    mailMsg.To.Add(email);
                }

                mailMsg.From = new MailAddress(_fromEmail, _fromName);
                mailMsg.Subject = subject;
                mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(message, null, MediaTypeNames.Text.Plain));

                _smtpClient.Send(mailMsg);
            }

        }

        public void Subscribe(EventTypes.Events eventType, List<string> toEmails)
        {
            if (toEmails == null) throw new NullReferenceException();

            _eventReceivers.Add(eventType, toEmails);
        }
    }
}
