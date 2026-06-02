using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Litle.Sdk
{

	public class Communications
    {
        private static readonly object SynLock = new object();
        public event EventHandler HttpAction;

        private void OnHttpAction(RequestType requestType, string xmlPayload, bool neuter)
        {
            var handler = HttpAction;
            if (handler != null)
            {
                if (neuter)
                {
                    NeuterXml(ref xmlPayload);
                }

                try
                {
                    handler(this, new HttpActionEventArgs(requestType, xmlPayload));
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("Litle SDK: HttpAction event subscriber threw: " + ex);
                }
            }
        }

        public static bool ValidateServerCertificate(
             object sender,
             X509Certificate certificate,
             X509Chain chain,
             SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Trace.TraceWarning("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private static readonly Regex CardNumberRegex = new Regex("(?i)<number>.*?</number>", RegexOptions.Compiled);
        private static readonly Regex AccNumRegex = new Regex("(?i)<accNum>.*?</accNum>", RegexOptions.Compiled);
        private static readonly Regex TrackRegex = new Regex("(?i)<track>.*?</track>", RegexOptions.Compiled);

        public void NeuterXml(ref string inputXml)
        {
            inputXml = CardNumberRegex.Replace(inputXml, "<number>xxxxxxxxxxxxxxxx</number>");
            inputXml = AccNumRegex.Replace(inputXml, "<accNum>xxxxxxxxxx</accNum>");
            inputXml = TrackRegex.Replace(inputXml, "<track>xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx</track>");
        }

        public void Log(string logMessage, string logFile, bool neuter)
        {
            lock (SynLock)
            {
                try
                {
                    if (neuter)
                    {
                        NeuterXml(ref logMessage);
                    }

                    using (var logWriter = new StreamWriter(logFile, true))
                    {
                        var time = DateTime.Now;
                        logWriter.WriteLine(time.ToString(CultureInfo.InvariantCulture));
                        logWriter.WriteLine(logMessage + "\r\n");
                    }
                }
                catch (IOException ex)
                {
                    Trace.TraceWarning("Litle SDK: Failed to write to log file '{0}': {1}", logFile, ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    Trace.TraceWarning("Litle SDK: Permission denied writing to log file '{0}': {1}", logFile, ex.Message);
                }
            }
        }

        public virtual Task<string> HttpPostAsync(string xmlRequest, Dictionary<string, string> config, CancellationToken cancellationToken)
        {
            return HttpPostCoreAsync(xmlRequest, config, cancellationToken);
        }

        public virtual string HttpPost(string xmlRequest, Dictionary<string, string> config)
        {
            // GetAwaiter().GetResult() avoids AggregateException wrapping that .Result produces
            return HttpPostCoreAsync(xmlRequest, config, CancellationToken.None).GetAwaiter().GetResult();
        }

        // TLS Configuration Note:
        // The original SDK explicitly set ServicePointManager.SecurityProtocol to TLS 1.1/1.2
        // for PCI compliance. This was intentionally removed during the .NET 10 upgrade because
        // .NET 10 defaults to TLS 1.2/1.3, which meets or exceeds PCI DSS requirements.
        // Hardcoding protocol versions is discouraged in modern .NET as it prevents the runtime
        // from negotiating the strongest mutually supported protocol.
        private async Task<string> HttpPostCoreAsync(string xmlRequest, Dictionary<string, string> config, CancellationToken cancellationToken)
        {
            string logFile = null;
            if (config.ContainsKey("logFile"))
            {
                logFile = config["logFile"];
            }

            if (!config.ContainsKey("url") || string.IsNullOrEmpty(config["url"]))
            {
                throw new LitleOnlineException("Required configuration parameter 'url' is missing or empty.");
            }
            var uri = config["url"];
            // WebRequest is obsolete (SYSLIB0014) but retained for backward compatibility;
            // migration to HttpClient requires broader architectural changes.
#pragma warning disable SYSLIB0014
            var request = (HttpWebRequest) WebRequest.Create(uri);
#pragma warning restore SYSLIB0014

            var neuter = false;
            if (config.ContainsKey("neuterAccountNums"))
            {
                neuter = ("true".Equals(config["neuterAccountNums"]));
            }

            var printxml = false;
            if (config.ContainsKey("printxml"))
            {
                if ("true".Equals(config["printxml"]))
                {
                    printxml = true;
                }
            }

            if (printxml)
            {
                Console.WriteLine(xmlRequest);
                Console.WriteLine(logFile);
            }

            //log request
            if (logFile != null)
            {
                Log(xmlRequest, logFile, neuter);
            }

            request.ContentType = "text/xml; charset=UTF-8";
            request.Method = "POST";
            request.ServicePoint.MaxIdleTime = 8000;
            request.ServicePoint.Expect100Continue = false;
            request.KeepAlive = false;

            int maxConnections = 10;
            if (config.ContainsKey("maxConnections"))
            {
                if (!int.TryParse(config["maxConnections"], out maxConnections) || maxConnections <= 0)
                {
                    Trace.TraceWarning("Litle SDK: Invalid maxConnections configuration '{0}', using default 10", config["maxConnections"]);
                    maxConnections = 10;
                }
            }
            request.ServicePoint.ConnectionLimit = maxConnections;

            int timeoutSec = 500;
            if (config.ContainsKey("timeout"))
            {
                if (!int.TryParse(config["timeout"], out timeoutSec) || timeoutSec <= 0)
                {
                    Trace.TraceWarning("Litle SDK: Invalid timeout configuration '{0}', using default 500s", config["timeout"]);
                    timeoutSec = 500;
                }
            }
            request.Timeout = timeoutSec * 1000;
            if (IsProxyOn(config))
            {
                int proxyPort;
                if (!int.TryParse(config["proxyPort"], out proxyPort) || proxyPort <= 0)
                {
                    throw new LitleOnlineException("Invalid proxy port configuration: " + config["proxyPort"]);
                }
                var myproxy = new WebProxy(config["proxyHost"], proxyPort)
                {
                    BypassProxyOnLocal = true
                };
                request.Proxy = myproxy;
            }

            OnHttpAction(RequestType.Request, xmlRequest, neuter);

            string xmlResponse;
            try
            {
                // submit http request
                using (var writer = new StreamWriter(await request.GetRequestStreamAsync().ConfigureAwait(false)))
                {
                    writer.Write(xmlRequest);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // read response
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var responseStream = response.GetResponseStream();
                    if (responseStream == null)
                    {
                        throw new LitleOnlineException("Received null response stream from server.");
                    }
                    using (var reader = new StreamReader(responseStream))
                    {
                        xmlResponse = (await reader.ReadToEndAsync().ConfigureAwait(false)).Trim();
                    }
                }
            }
            catch (WebException we)
            {
                string detail = "";
                if (we.Response != null)
                {
                    try
                    {
                        using (var errorResponse = we.Response)
                        using (var errorStream = errorResponse.GetResponseStream())
                        {
                            if (errorStream != null)
                            {
                                using (var errorReader = new StreamReader(errorStream))
                                {
                                    detail = errorReader.ReadToEnd();
                                }
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Trace.TraceWarning("Litle SDK: Failed to read error response body: " + innerEx.Message);
                    }
                }
                throw new LitleOnlineException(
                    "HTTP request failed for URL '" + uri + "': " + we.Message +
                    (string.IsNullOrEmpty(detail) ? "" : " Response body: " + detail), we);
            }
            catch (OperationCanceledException oce)
            {
                throw new LitleOnlineException("HTTP request was cancelled or timed out for URL: " + uri, oce);
            }
            catch (IOException ioe)
            {
                throw new LitleOnlineException("Network I/O error: " + ioe.Message, ioe);
            }
            if (printxml)
            {
                Console.WriteLine(xmlResponse);
            }

            OnHttpAction(RequestType.Response, xmlResponse, neuter);

            //log response
            if (logFile != null)
            {
                Log(xmlResponse, logFile, neuter);
            }

            return xmlResponse;
        }

        public bool IsProxyOn(Dictionary<string, string> config)
        {
            return config.ContainsKey("proxyHost") && !string.IsNullOrEmpty(config["proxyHost"]) && config.ContainsKey("proxyPort") && !string.IsNullOrEmpty(config["proxyPort"]);
        }
    }

    public enum RequestType
    {
        Request, Response
    }

    public class HttpActionEventArgs : EventArgs
    {
        public RequestType RequestType { get; }
        public string XmlPayload { get; }

        public HttpActionEventArgs(RequestType requestType, string xmlPayload)
        {
            RequestType = requestType;
            XmlPayload = xmlPayload;
        }
    }

}
