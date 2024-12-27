using Entities.RequestModels;
using Logic.ILogic;
using Logic.Logic;
using WebApiMariaMC.IServicies;

namespace WebApiMariaMC.Servicies
{
    public class SendMailService: ISendMailService
    {
        private readonly ISendMailLogic _sendMailLogic;
        public SendMailService(ISendMailLogic sendMaillogic)
        {
            _sendMailLogic = sendMaillogic;
        }

        public Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath="") { 
            return _sendMailLogic.SendEmailAsync(toEmail, subject, body, attachmentPath);
        }

    }


}
