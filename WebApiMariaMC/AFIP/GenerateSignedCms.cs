using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace WebApiMariaMC.AFIP
{
    public class CmsGenerator
    {
        public string GenerateSignedCms(string loginTicketRequest, string certPath, string keyPath)
        {
            //// Cargar el certificado (en formato PEM)
            //var cert = new X509Certificate2(certPath);

            //// Cargar la clave privada desde el archivo PEM
            ////RSA privateKey;
            ////var rsa = RSA.Create();
            //using (var rsa = RSA.Create())
            //{
            //    var keyData = System.IO.File.ReadAllText(keyPath);
            //    rsa.ImportFromPem(keyData.ToCharArray());
            //    // Asignar la clave privada al certificado
            //    cert = cert.CopyWithPrivateKey(rsa);
            //}

            //// Asignar la clave privada al certificado
            ////cert = cert.CopyWithPrivateKey(rsa);

            //// Convertir el XML a bytes
            //var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(loginTicketRequest));

            //// Crear un objeto SignedCms
            //var signedCms = new SignedCms(contentInfo, true);

            //// Crear un firmante CmsSigner
            //var cmsSigner = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, cert)
            //{
            //    IncludeOption = X509IncludeOption.EndCertOnly
            //};

            //// Firmar el CMS
            //signedCms.ComputeSignature(cmsSigner);

            //// Codificar el CMS a Base64
            //var encodedCms = Convert.ToBase64String(signedCms.Encode());

            //// Formatear como CMS con encabezado y pie de página
            //var formattedCms = $"-----BEGIN CMS-----\n{encodedCms}\n-----END CMS-----";

            X509Certificate2 certFirmante = ObtenerCertificadoDesdeArchivos(certPath, keyPath);

            byte[] msgBytes = Encoding.UTF8.GetBytes(loginTicketRequest);
            byte[] encodedSignedCms = FirmarMensaje(msgBytes, certFirmante);

            string cmsFirmadoBase64 = Convert.ToBase64String(encodedSignedCms);


            return cmsFirmadoBase64;
        }

        public static X509Certificate2 ObtenerCertificadoDesdeArchivos(string certPath, string keyPath)
        {
            // Cargar el certificado desde el archivo PEM
            var cert = new X509Certificate2(certPath);

            // Leer la clave privada desde el archivo y asignarla al certificado
            string privateKeyText = File.ReadAllText(keyPath);

            var privateKey = RSA.Create();
            privateKey.ImportFromPem(privateKeyText.ToCharArray());

            // Crear un nuevo certificado que incluya la clave privada
            cert = cert.CopyWithPrivateKey(privateKey);

            return cert;
        }

        public static byte[] FirmarMensaje(byte[] msgBytes, X509Certificate2 certFirmante)
        {
            var contentInfo = new ContentInfo(msgBytes);
            var signedCms = new SignedCms(contentInfo);
            var cmsFirmante = new CmsSigner(certFirmante) { IncludeOption = X509IncludeOption.EndCertOnly };
            signedCms.ComputeSignature(cmsFirmante);
            return signedCms.Encode();
        }

        public static RSA DecodePrivateKey(string pemKeyContent)
        {
            // Implementación para leer la clave privada en formato PEM
            return RSA.Create(); // Placeholder
        }

    }


}
