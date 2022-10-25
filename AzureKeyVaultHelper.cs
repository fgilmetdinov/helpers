using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Noname.Client.Helpers
{
    //https://c-sharx.net/read-secrets-from-azure-key-vault-in-a-net-core-console-app
    public class AzureKeyVaultHelper
    {
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly KeyVaultClient keyVaultClient;

        public AzureKeyVaultHelper(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
        }

        public string GetSecret(string url)
        {
            var secretBundle = Task.Run(() => keyVaultClient.GetSecretAsync(url))
                .ConfigureAwait(false).GetAwaiter().GetResult();
            return secretBundle.Value;
        }

        private async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(clientId, clientSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        public byte[] GetCertificate(string urlCertificate, bool withPrivateKey = true)
        {
            byte[] result;
            var cert = Task.Run(() => keyVaultClient.GetCertificateAsync(urlCertificate))
                .ConfigureAwait(false).GetAwaiter().GetResult();

            if (withPrivateKey)
            {
                var certificateSecret = Task.Run(() => keyVaultClient.GetSecretAsync(cert.SecretIdentifier.Identifier))
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                result = Convert.FromBase64String(certificateSecret.Value);
            }
            else
            {
                result = cert.Cer;
            }

            return result;            
        }
    }
}
