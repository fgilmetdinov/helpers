using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Noname.Client.Helpers
{
    public static class CertificateHelper
    {
        public static (string certificate, string privateKey) GetCertificateKeys(byte[] certificateData)
        {
            var certificate = new X509Certificate2(certificateData);

            var privateKey = "";
            var certificateBuilder = new StringBuilder();
            certificateBuilder.Append(CreateCertificate(certificate.RawData));

            var collection = new X509Certificate2Collection();
            collection.Import(certificateData, null, X509KeyStorageFlags.Exportable);
            foreach (var cert in collection)
            {
                if (cert.HasPrivateKey)
                {
                    privateKey = ExportPrivateKey(certificate.PrivateKey as RSACng);
                }
                else
                {
                    certificateBuilder.Append(CreateCertificate(cert.RawData));
                }
            }
            
            return (certificateBuilder.ToString(), privateKey);
        }

        private static string CreateCertificate(byte[] data)
        {
            StringWriter outputStream = new StringWriter();
            var base64 = Convert.ToBase64String(data).ToCharArray();
            outputStream.Write("-----BEGIN CERTIFICATE-----\n");
            // Output as Base64 with lines chopped at 64 characters
            for (var i = 0; i < base64.Length; i += 64)
            {
                outputStream.Write(base64, i, Math.Min(64, base64.Length - i));
                outputStream.Write("\n");
            }
            outputStream.Write("-----END CERTIFICATE-----\n");

            return outputStream.ToString();
        }

        /// <summary>
        /// Export private (including public) key from MS RSACryptoServiceProvider into OpenSSH PEM string
        /// slightly modified from https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="csp"></param>
        /// <returns></returns>
        private static string ExportPrivateKey(RSACryptoServiceProvider csp)
        {            
            if (csp.PublicOnly) throw new ArgumentException("CSP does not contain a private key", "csp");
            var parameters = csp.ExportParameters(true);
            return ExportPrivateKey(parameters);
        }

        private static string ExportPrivateKey(RSACng rsaCng)
        {
            var rsa = (RSA)rsaCng;
            (rsaCng as RSACng).Key.SetProperty(
                new CngProperty("Export Policy",
                    BitConverter.GetBytes((int)CngExportPolicies.AllowPlaintextExport),
                    CngPropertyOptions.Persist));
            var parameters = rsa.ExportParameters(true);
            return ExportPrivateKey(parameters);
        }

        private static string ExportPrivateKey(RSAParameters parameters)
        {
            StringWriter outputStream = new StringWriter();
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryWriter(stream);
                writer.Write((byte)0x30); // SEQUENCE
                using (var innerStream = new MemoryStream())
                {
                    var innerWriter = new BinaryWriter(innerStream);
                    EncodeIntegerBigEndian(innerWriter, new byte[] { 0x00 }); // Version
                    EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
                    EncodeIntegerBigEndian(innerWriter, parameters.D);
                    EncodeIntegerBigEndian(innerWriter, parameters.P);
                    EncodeIntegerBigEndian(innerWriter, parameters.Q);
                    EncodeIntegerBigEndian(innerWriter, parameters.DP);
                    EncodeIntegerBigEndian(innerWriter, parameters.DQ);
                    EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
                    var length = (int)innerStream.Length;
                    EncodeLength(writer, length);
                    writer.Write(innerStream.GetBuffer(), 0, length);
                }

                var base64 = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length).ToCharArray();
                // WriteLine terminates with \r\n, we want only \n
                outputStream.Write("-----BEGIN RSA PRIVATE KEY-----\n");
                // Output as Base64 with lines chopped at 64 characters
                for (var i = 0; i < base64.Length; i += 64)
                {
                    outputStream.Write(base64, i, Math.Min(64, base64.Length - i));
                    outputStream.Write("\n");
                }
                outputStream.Write("-----END RSA PRIVATE KEY-----");
            }

            return outputStream.ToString();
        }

        /// <summary>
        /// https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="length"></param>
        private static void EncodeLength(BinaryWriter stream, int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException("length", "Length must be non-negative");
            if (length < 0x80)
            {
                // Short form
                stream.Write((byte)length);
            }
            else
            {
                // Long form
                var temp = length;
                var bytesRequired = 0;
                while (temp > 0)
                {
                    temp >>= 8;
                    bytesRequired++;
                }
                stream.Write((byte)(bytesRequired | 0x80));
                for (var i = bytesRequired - 1; i >= 0; i--)
                {
                    stream.Write((byte)(length >> (8 * i) & 0xff));
                }
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/23739932/2860309
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="value"></param>
        /// <param name="forceUnsigned"></param>
        private static void EncodeIntegerBigEndian(BinaryWriter stream, byte[] value, bool forceUnsigned = true)
        {
            stream.Write((byte)0x02); // INTEGER
            var prefixZeros = 0;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] != 0) break;
                prefixZeros++;
            }
            if (value.Length - prefixZeros == 0)
            {
                EncodeLength(stream, 1);
                stream.Write((byte)0);
            }
            else
            {
                if (forceUnsigned && value[prefixZeros] > 0x7f)
                {
                    // Add a prefix zero to force unsigned if the MSB is 1
                    EncodeLength(stream, value.Length - prefixZeros + 1);
                    stream.Write((byte)0);
                }
                else
                {
                    EncodeLength(stream, value.Length - prefixZeros);
                }
                for (var i = prefixZeros; i < value.Length; i++)
                {
                    stream.Write(value[i]);
                }
            }
        }

    }
}
