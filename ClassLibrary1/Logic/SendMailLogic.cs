using System.Net.Mail;
using System.Net;
using Logic.ILogic;
using DinkToPdf;
using DinkToPdf.Contracts;
using MimeKit;
using MailKit.Security;
using Newtonsoft.Json.Linq;
using Data.Models;
using Microsoft.Extensions.Configuration;

namespace Logic.Logic
{
    public class SendMailLogic: ISendMailLogic
    {
        private readonly IConfiguration _configuration;

        public SendMailLogic(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("tomasgdhreg@gmail.com", "ecpz cjao sqlr crub"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("tomasgdh@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(toEmail);

            // Adjuntar el PDF
            if (!string.IsNullOrEmpty(attachmentPath))
            {
                mailMessage.Attachments.Add(new Attachment(attachmentPath));
            }

            await smtpClient.SendMailAsync(mailMessage);
        }
        public async Task SendEmailFacturaWithOAuth2Async(string recipientEmail, string subject, string body, string htmlContent)
        {
            string mail = _configuration["Mail:clientId"];
            string clientSecret = _configuration["Mail:clientSecret"];
            string refreshToken = _configuration["Mail:refreshToken"];
            var accessToken = await GetAccessTokenAsync(refreshToken, mail, clientSecret);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Maria Moda Circular", mail));
            message.To.Add(new MailboxAddress("", recipientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { TextBody = body };

            // Convert HTML string to PDF
            byte[] pdfBytes = ConvertHtmlToPdf(htmlContent);
            if (pdfBytes != null)
            {
                // Add the PDF as an attachment
                bodyBuilder.Attachments.Add("factura.pdf", pdfBytes, new ContentType("application", "pdf"));
            }

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(new SaslMechanismOAuth2(mail, accessToken));
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
        public static byte[] ConvertHtmlToPdf(string htmlContent)
        {
            var converter = new SynchronizedConverter(new PdfTools());
            var doc = new HtmlToPdfDocument
            {
                GlobalSettings = {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4
            },
                Objects = {
                new ObjectSettings {
                    PagesCount = true,
                    HtmlContent = htmlContent,
                    WebSettings = { DefaultEncoding = "utf-8" }
                }
            }
            };

            return converter.Convert(doc);
        }
        private static async Task<string> GetAccessTokenAsync(string refreshToken, string clientId, string clientSecret)
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
                {
                    Content = new FormUrlEncodedContent(new[]
                    {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("refresh_token", refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            })
                };

                var response = await client.SendAsync(request);
                var json = await response.Content.ReadAsStringAsync();
                var tokenResponse = JObject.Parse(json);

                return tokenResponse["access_token"]?.ToString();
            }
        }

    }
}
