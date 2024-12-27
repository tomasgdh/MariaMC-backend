using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.ILogic
{
    public interface ISendMailLogic
    {
        Task SendEmailAsync(string toEmail, string subject, string body, string attachmentPath);
    }
}
