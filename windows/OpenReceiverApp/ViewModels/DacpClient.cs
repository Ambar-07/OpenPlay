using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenReceiver.ViewModels
{
    public static class DacpClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        public static string ActiveRemote { get; set; }
        public static string ClientIp { get; set; }

        public static async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(ActiveRemote) || string.IsNullOrEmpty(ClientIp))
                return;

            try
            {
                // DACP commands are sent to the client IP on port 3689
                string url = $"http://{ClientIp}:3689/ctrl-int/1/{command}?Active-Remote={ActiveRemote}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Viewer-Only-Client", "1");
                request.Headers.Add("Client-DAAP-Version", "3.11");
                
                var response = await _httpClient.SendAsync(request);
                System.Diagnostics.Debug.WriteLine($"DACP {command} response: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DACP Error: {ex.Message}");
            }
        }
    }
}
