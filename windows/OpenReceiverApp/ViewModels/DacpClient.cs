using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace OpenReceiver.ViewModels
{
    public static class DacpClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int _resolvedPort = -1;
        
        public static string ActiveRemote { get; set; }
        public static string ClientIp { get; set; }
        public static string DacpId { get; set; }

        public static async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrEmpty(ActiveRemote) || string.IsNullOrEmpty(ClientIp) || string.IsNullOrEmpty(DacpId))
                return;

            try
            {
                if (_resolvedPort == -1)
                {
                    // Resolve the DACP port via mDNS
                    var results = await Zeroconf.ZeroconfResolver.ResolveAsync("_dacp._tcp.local.");
                    foreach (var host in results)
                    {
                        if (host.IPAddress == ClientIp)
                        {
                            foreach (var svc in host.Services.Values)
                            {
                                _resolvedPort = svc.Port;
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Resolved DACP port for {ClientIp} to {_resolvedPort}");
                                break;
                            }
                            if (_resolvedPort != -1) break;
                        }
                    }
                    
                    if (_resolvedPort == -1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Failed to resolve DACP port for {ClientIp}. Falling back to 3689.");
                        _resolvedPort = 3689; // Fallback
                    }
                }

                // DACP commands are sent to the client IP on the resolved port
                string url = $"http://{ClientIp}:{_resolvedPort}/ctrl-int/1/{command}";
                
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Viewer-Only-Client", "1");
                request.Headers.Add("Client-DAAP-Version", "3.11");
                request.Headers.Add("Active-Remote", ActiveRemote);
                
                var response = await _httpClient.SendAsync(request);
                string responseText = $"[{DateTime.Now:HH:mm:ss.fff}] DACP {command} response: {response.StatusCode}";
                System.Diagnostics.Debug.WriteLine(responseText);
                try { System.IO.File.AppendAllText(@"C:\Users\ambar\OneDrive\Desktop\airplay.log", responseText + "\n"); } catch {}
            }
            catch (Exception ex)
            {
                string errorText = $"[{DateTime.Now:HH:mm:ss.fff}] DACP Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(errorText);
                try { System.IO.File.AppendAllText(@"C:\Users\ambar\OneDrive\Desktop\airplay.log", errorText + "\n"); } catch {}
            }
        }

        public static async Task SetPropertyAsync(string prop, string value)
        {
            await SendCommandAsync($"setproperty?{prop}={value}");
        }
    }
}
