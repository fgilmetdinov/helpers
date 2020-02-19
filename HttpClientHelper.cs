using RestSharp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rw.Barracuda.Client.Helpers
{
    internal class HttpClientHelper
    {
        private readonly RestClient client;
        public string Token { get; set; }

        public HttpClientHelper(string baseAddress)
        {
            client = new RestClient(baseAddress);
        }      

        public async Task<string> Get(string path)
        {
            return await SendRequest(path, Method.GET);
        }

        public async Task<string> Patch(string path, string json)
        {
            return await SendRequest(path, Method.PATCH,
                new Dictionary<string, string> { { "application/json", json } });
        }

        public async Task<string> Post(string path, string json)
        {
            return await SendRequest(path, Method.POST,
                new Dictionary<string, string> { { "application/json", json } });
        }
        public async Task<string> Post(string path, Dictionary<string, string> parameters)
        {
            return await SendRequest(path, Method.POST, parameters);
        }

        public async Task<string> Delete(string path)
        {
            return await SendRequest(path, Method.DELETE);
        }

        private async Task<string> SendRequest(string path, Method method, Dictionary<string, string> parameters = null)
        {
            var request = new RestRequest(path, method, DataFormat.Json);
            request.AddHeader("auth-api", Token);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    if (!string.IsNullOrEmpty(param.Value))
                        request.AddParameter(param.Key, param.Value, ParameterType.RequestBody);
                }
            }

            var response = await client.ExecuteAsync(request);
            if (response.ErrorException != null)
                throw response.ErrorException;
            if (!string.IsNullOrEmpty(response.ErrorMessage))
                throw new System.Exception(response.ErrorMessage);

            return response.Content;
        }

    }
}
