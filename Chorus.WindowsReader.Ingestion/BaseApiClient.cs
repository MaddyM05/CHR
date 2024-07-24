using Chorus.WindowsReader.Common;
using Chorus.WindowsReader.Common.Logger;
using Google.Protobuf;
using Newtonsoft.Json;
using Polly;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Chorus.WindowsReader.Ingestion
{
    public class BaseApiClient
    {
        private readonly IChorusLogger<BaseApiClient> _logger;
        public BaseApiClient(IChorusLogger<BaseApiClient> logger)
        {
            _logger = logger;
        }
        public async Task<string> SendPayloadAsync(Payload payload)
        {
            var retryPolicy = Polly.Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(5),
            (exception, timeSpan, retryCount, context) =>
            {
                _logger.LogException(new ChorusCustomException($"Retry {retryCount} encountered an error: {exception.Message}"));
            });

            return await retryPolicy.ExecuteAsync(async () =>
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        string url = string.Empty;
                        HttpResponseMessage response = new HttpResponseMessage();
                        if (GlobalHelper.AppSettings.UploadThroughJson)
                        {
                            url = GlobalHelper.AppSettings.ApiEndpointForJson;
                            var jsonPayload = JsonConvert.SerializeObject(payload);
                            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                            response = await httpClient.PostAsync(url, content);
                        }
                        else
                        {
                            url = GlobalHelper.AppSettings.ApiEndpointForProto;
                            var byteArray = payload.ToByteArray();
                            var content = new ByteArrayContent(byteArray);
                            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            content.Headers.ContentType.CharSet = "utf-8";
                            response = await httpClient.PostAsync(url, content);
                        }

                        response.EnsureSuccessStatusCode();

                        var responseContent = await response.Content.ReadAsStringAsync();
                        if (string.IsNullOrWhiteSpace(responseContent))
                        {
                            return "Payload uploaded via PROTOBUF from device:" + $"{payload.DeviceId}";
                        }
                        else if (responseContent.Contains("{}"))
                        {
                            return "Payload uploaded via JSON from device:" + $"{payload.DeviceId}";
                        }
                        else
                        {
                            return responseContent;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    // Handle HTTP request exceptions
                    _logger.LogException(new ChorusCustomException("Error during HTTP request: " + ex.Message, ex));
                    throw new Exception("Error during HTTP request: " + ex.Message, ex);
                }
                catch (TaskCanceledException ex)
                {
                    // Handle task cancellations (e.g., due to timeouts)
                    _logger.LogException(new ChorusCustomException("HTTP request timed out: " + ex.Message, ex));
                    throw new Exception("HTTP request timed out: " + ex.Message, ex);
                }
                catch (TimeoutException ex)
                {
                    // Handle timeouts
                    _logger.LogException(new ChorusCustomException("HTTP request timed out: " + ex.Message, ex));
                    throw new Exception("HTTP request timed out: " + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    // Handle other types of exceptions
                    _logger.LogException(new ChorusCustomException("An unexpected error occurred: " + ex.Message, ex));
                    throw new Exception("An unexpected error occurred: " + ex.Message, ex);
                }
            });
        }
    }
}
