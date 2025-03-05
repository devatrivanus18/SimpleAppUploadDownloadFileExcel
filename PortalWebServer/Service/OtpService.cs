using MailKit.Net.Smtp;
using MimeKit;
using System.Collections.Concurrent;

namespace PortalWebServer.Service
{
    public class OtpService
    {
        private static readonly ConcurrentDictionary<string, string> OtpStore = new();

        public string GenerateOtp(string email)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            OtpStore[email] = otp;
            return otp;
        }

        public bool VerifyOtp(string email, string otp)
        {
            return OtpStore.TryGetValue(email, out var storedOtp) && storedOtp == otp;
        }

        public void SendEmail(string email, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Portal Web", "devadlsc18@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Your OTP Code";
            message.Body = new TextPart("plain") { Text = $"Your OTP code is: {otp}" };

            using var client = new SmtpClient();
            client.Connect("smtp.gmail.com", 587, false);
            client.Authenticate("devadlsc18@gmail.com", "wbff pjyx ejju fkdu");
            client.Send(message);
            client.Disconnect(true);
        }
    }
}
