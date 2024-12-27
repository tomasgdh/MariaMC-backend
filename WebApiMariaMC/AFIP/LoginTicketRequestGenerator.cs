using System;
using System.Xml.Linq;

namespace WebApiMariaMC.AFIP
{
    public class LoginTicketRequestGenerator
    {
        public string GenerateLoginTicketRequest(string service)
        {
            var uniqueId = 3;//DateTime.Now.ToString("yyMMddHHmmss");
            var generationTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            var expirationTime = DateTime.Now.AddMinutes(20).ToString("yyyy-MM-ddTHH:mm:ss");

            var xml = new XElement("loginTicketRequest",
                new XElement("header",
                    new XElement("uniqueId", uniqueId),
                    new XElement("generationTime", generationTime),
                    new XElement("expirationTime", expirationTime)
                ),
                new XElement("service", service)
            );

            return xml.ToString();
        }
    }

}
