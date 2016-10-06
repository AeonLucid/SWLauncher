using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using SWLauncher.Extensions;
using SWLauncher.Forms;

namespace SWLauncher.Proxies
{
    /// <summary>
    ///     Scrapes japanese proxies and tests them.
    /// </summary>
    internal class ProxyManager
    {
        private readonly FrmMain _frmMain;

        private readonly HttpClient _httpClient;

        private readonly HtmlParser _htmlParser;

        public ProxyManager(FrmMain frmMain)
        {
            _frmMain = frmMain;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
            _htmlParser = new HtmlParser();
        }

        public Dictionary<string, IWebProxy> Proxies { get; private set; } = new Dictionary<string, IWebProxy>();

        /// <summary>
        ///     Scrapes fresh japanese proxies.
        /// </summary>
        /// <returns>Returns true if one or more proxies has been scraped.</returns>
        public async Task<bool> ScrapeProxiesAsync(int minimalUptime)
        {
            Proxies = new Dictionary<string, IWebProxy>();

            using (var response = _httpClient.PostAsync("http://gatherproxy.com/proxylist/country/?c=Japan", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"Filter", ""},
                {"Uptime", $"{minimalUptime}"}, // Only want ..% uptime proxies to filter out the ones that are mostly down.
                {"Country", "japan"}
            })))
            {
                var pageSource = await response.Result.Content.ReadAsStringAsync();
                var document = _htmlParser.Parse(pageSource);
                var proxyRows = document.QuerySelectorAll("#tblproxy tr");

                foreach (var proxyRow in proxyRows)
                {
                    var columns = proxyRow.QuerySelectorAll("td");
                    if (columns.Length <= 2) continue;

                    var ip = columns[1].QuerySelector("script").InnerHtml.Between("'", "'");
                    var port = int.Parse(columns[2].QuerySelector("script").InnerHtml.Between("'", "'"), NumberStyles.HexNumber); // hex

                    Proxies.Add($"{ip}:{port}", new WebProxy(ip, port));
                }
            }

            if (Proxies.Count > 1)
                Proxies = Proxies.Shuffle();

            return Proxies.Count > 0;
        }

        /// <summary>
        ///     Gets the first working proxy and removes the ones that don't work.
        /// </summary>
        /// <returns>Returns a working <see cref="IWebProxy"/> or null if none were found.</returns>
        public async Task<IWebProxy> GetFirstWorkingProxy()
        {
            IWebProxy workingProxy = null;

            var removeProxies = new List<string>();

            foreach (var proxy in Proxies)
            {
                _frmMain.UpdateStatus($"Checking proxy {proxy.Key}");

                if (await TestProxy(proxy.Key.Split(':')[0], proxy.Value))
                {
                    Console.WriteLine($"Working proxy: {proxy.Key}");

                    workingProxy = proxy.Value;
                    break;
                }

                removeProxies.Add(proxy.Key);

                Console.WriteLine($"Defect proxy: {proxy.Key}");
            }

            foreach (var proxy in removeProxies)
                Proxies.Remove(proxy);
            
            return workingProxy;
        }

        public void RemoveProxy(IWebProxy webProxy)
        {
            if (Proxies.ContainsValue(webProxy))
                Proxies.Remove(Proxies.First(x => x.Value == webProxy).Key);
        }

        /// <summary>
        ///     Tests if the given proxy actually works.
        /// </summary>
        /// <param name="ip">The remote ip address of the proxy.</param>
        /// <param name="webProxy">The proxy.</param>
        /// <returns>Returns true if the proxy works.</returns>
        private static async Task<bool> TestProxy(string ip, IWebProxy webProxy)
        {
            using (var webclient = new WebClient())
            {
                webclient.Proxy = webProxy;

                try
                {
                    var clientIp = await webclient.DownloadStringTaskAsync("http://canihazip.com/s");

                    return clientIp.Equals(ip);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }
    }
}
