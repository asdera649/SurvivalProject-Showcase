using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Ping = System.Net.NetworkInformation;

namespace SP.Runtime.Bootstrap.Services.BackendInteractionService
{
    public class BackendInteractionService
    {
        #region Structs
        
        public enum RequestResult
        {
            Successful,
            Obsolete,
            NoConnection,
            UnknownError
        }
        
        public class ServerRegistrationResult
        {
            public RequestResult RequestResult { get; set; }
            public ServerRegistrationData ServerRegistrationData { get; set; }
        }
        
        public class ServerRegistrationData
        {
            public string EndPoint { get; set; }
            public string BucketName { get; set; }
            public string ObjectKey { get; set; }
            public string AccessKey { get; set; }
            public string SecretKey { get; set; }
        }

        public class ServerList
        {
            public RequestResult RequestResult { get; set; }
            public List<ServerInfo> Servers { get; set; } = new();
        }
        
        // ReSharper disable once ClassNeverInstantiated.Global
        public class ServerInfo
        {
            public string Ip { get; set; }
            public string UniqueId { get; set; }
            public string Name { get; set; }
            public int MaxPlayersCount { get; set; }
            public int CurrentPlayersCount { get; set; }
        }

        public class ServerConnectionInfo
        {
            public RequestResult RequestResult { get; set; } 
            public ConnectionInfo ConnectionInfo { get; set; }
        }
        
        public class ConnectionInfo
        {
            public string PublicIp { get; set; }
            public ushort ExternalPort { get; set; }
        }
        
        public class ServerState
        {
            public int MaxPlayersCount { get; set; }
            public int CurrentPlayersCount { get; set; }
        }
        
        public class RemainingTimeToWipe
        {
            public int Days { get; set; }
            public int Hours { get; set; }
            public int Minutes { get; set; }
        }
        
        #endregion
        
        public BackendInteractionService(string backendUrl, bool disableVersionChecking = false)
        {
            _url = backendUrl;
            _disableVersionChecking = disableVersionChecking;
        }

        private readonly string _url;
        private readonly bool _disableVersionChecking;

        private readonly HttpClient _httpClient = new();
        
        public async Task<RequestResult> CheckVersion()
        {
            if (_disableVersionChecking)
            {
                return RequestResult.Successful;
            }
            
            try
            {
                var response = await _httpClient.GetAsync($"{_url}/actualGameClientData/currentVersion");
        
                if (!response.IsSuccessStatusCode)
                {
                    return RequestResult.UnknownError;
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                return Application.version != responseContent ? 
                    RequestResult.Obsolete :
                    RequestResult.Successful;
            }
            catch (HttpRequestException)
            {
                return RequestResult.NoConnection;
            }
            catch (Exception)
            {
                return RequestResult.UnknownError;
            }
        }

        public async Task<ServerRegistrationResult> RegisterServer()
        {
            var output = new ServerRegistrationResult();
            
            var requestId = Environment.GetEnvironmentVariable("ARBITRIUM_REQUEST_ID");

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_url}/serversManagement/registerServer?requestId={requestId}");
            
                if (!response.IsSuccessStatusCode)
                {
                    output.RequestResult = RequestResult.UnknownError;
                    
                    if (response.StatusCode == HttpStatusCode.UpgradeRequired)
                    {
                        output.RequestResult = RequestResult.Obsolete;
                    }

                    return output;
                }
            
                var responseContent = await response.Content.ReadAsStringAsync();
                
                output.ServerRegistrationData = JsonConvert.DeserializeObject<ServerRegistrationData>(responseContent);
                
                output.RequestResult = RequestResult.Successful;
            }
            catch (HttpRequestException)
            {
                output.RequestResult = RequestResult.NoConnection;
            }
            catch (Exception)
            {
                output.RequestResult = RequestResult.UnknownError;
            }

            return output;
        }
        
        public async Task<bool> SetServerReady(bool isLog = true)
        {
            var requestId = Environment.GetEnvironmentVariable("ARBITRIUM_REQUEST_ID");

            try
            {
                var content = new StringContent("", Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(
                    $"{_url}/serversManagement/setServerReady?requestId={requestId}", content);
            
                if (!response.IsSuccessStatusCode)
                {
                    if (isLog)
                    {
                        Debug.LogWarning($"SetServerReady Error: {response.StatusCode} - {response.ReasonPhrase}");
                    }
                    
                    return false;
                }
            }
            catch (Exception ex)
            {
                if (isLog)
                {
                    Debug.LogWarning($"SetServerReady Error: {ex.Message}");
                }
                
                return false;
            }

            return true;
        }

        public async Task<ServerList> GetServers()
        {
            var output = new ServerList();
            
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_url}/serversManagement/servers?clientVersion={Application.version}");
            
                if (!response.IsSuccessStatusCode)
                {
                    output.RequestResult = RequestResult.UnknownError;
                    
                    if (response.StatusCode == HttpStatusCode.UpgradeRequired)
                    {
                        output.RequestResult = RequestResult.Obsolete;
                    }
                    
                    return output;
                }
            
                var responseContent = await response.Content.ReadAsStringAsync();
                
                output.Servers = JsonConvert.DeserializeObject<List<ServerInfo>>(responseContent);

                output.RequestResult = RequestResult.Successful;
            }
            catch (HttpRequestException)
            {
                output.RequestResult = RequestResult.NoConnection;
            }
            catch (Exception)
            {
                output.RequestResult = RequestResult.UnknownError;
            }

            return output;
        }

        public async Task<ServerConnectionInfo> GetServerConnectionInfo(string uniqueId)
        {
            var output = new ServerConnectionInfo();

            try
            {
                var response = await _httpClient.GetAsync(
                    $"{_url}/serversManagement/connect?uniqueId={uniqueId}&clientVersion={Application.version}");
            
                if (!response.IsSuccessStatusCode)
                {
                    output.RequestResult = RequestResult.UnknownError;
                    
                    if (response.StatusCode == HttpStatusCode.UpgradeRequired)
                    {
                        output.RequestResult = RequestResult.Obsolete;
                    }
                    
                    return output;
                }
            
                var responseContent = await response.Content.ReadAsStringAsync();
                
                output.ConnectionInfo = JsonConvert.DeserializeObject<ConnectionInfo>(responseContent);

                output.RequestResult = RequestResult.Successful;
            }
            catch (HttpRequestException)
            {
                output.RequestResult = RequestResult.NoConnection;
            }
            catch (Exception)
            {
                output.RequestResult = RequestResult.UnknownError;
            }
            
            return output;
        }

        public async Task<bool> TrySendServerInfo(ServerState serverState)
        {
            var requestId = Environment.GetEnvironmentVariable("ARBITRIUM_REQUEST_ID");
            
            try
            {
                var json = JsonConvert.SerializeObject(serverState);
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(
                    $"{_url}/serversManagement/updateServerState?requestId={requestId}",
                    content);
                
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"TrySendServerInfo Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"TrySendServerInfo Error: {ex.Message}");
                return false;
            }

            return true;
        }
        
        public async Task<RemainingTimeToWipe> GetRemainingTimeToWipe()
        {
            var output = new RemainingTimeToWipe
            {
                Days = -1,
                Hours = -1,
                Minutes = -1
            };

            try
            {
                var response = await _httpClient.GetAsync($"{_url}/serversWipe/remainingTimeToWipe");
        
                if (!response.IsSuccessStatusCode)
                {
                    Debug.LogWarning($"GetRemainingTimeToWipe Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return output;
                }

                var responseContent = await response.Content.ReadAsStringAsync();

                output = JsonConvert.DeserializeObject<RemainingTimeToWipe>(responseContent);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GetRemainingTimeToWipe Error: {ex.Message}");
            }

            return output;
        }
        
        public async Task<Dictionary<ServerInfo, long>> GetServersPing(IEnumerable<ServerInfo> servers)
        {
            var pingTasks = servers.Select(async server =>
            {
                var ping = await GetPing(server);
                
                return new KeyValuePair<ServerInfo, long>(server, ping);
            });
            
            return (await Task.WhenAll(pingTasks)).ToDictionary(pair => pair.Key, pair => pair.Value);
        }
        
        private async Task<long> GetPing(ServerInfo server)
        {
            try
            {
                using var ping = new Ping.Ping();
                
                var reply = await ping.SendPingAsync(server.Ip);

                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime;
                }
                
                return -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}