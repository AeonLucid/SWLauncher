using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MadMilkman.Ini;
using SWLauncher.Extensions;
using SWLauncher.Proxies;

namespace SWLauncher.Forms
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
        }

        private async void btnLaunch_Click(object sender, EventArgs e)
        {
            UpdateStatus("Scraping proxies");

            var proxyManager = new ProxyManager(this);
            if (await proxyManager.ScrapeProxiesAsync(trackBarUptime.Value))
            {
                var cookieContainer = new CookieContainer();

                // 1: Login to hangame site.
                UpdateStatus("Logging in to hangame.co.jp");

                using (var httpClient = new HttpClient(new HttpClientHandler
                {
                    CookieContainer = cookieContainer
                }))
                {
                    using (var loginResponse = await httpClient.PostAsync("https://id.hangame.co.jp/login.nhn", new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        {"strmemberid", textBoxUsername.Text},
                        {"strpassword", textBoxPassword.Text}
                    })))
                    {
                        if (!loginResponse.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Couldn't authenticate.", "Woops..", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            return;
                        }

                        var response = await loginResponse.Content.ReadAsStringAsync();
                        if (response.Contains("var msg"))
                        {
                            var message = response.Between("'", "'");

                            if (message.Contains("画像"))
                            {
                                MessageBox.Show("You have used an incorrect password too much and triggered captcha.\nTry to login manually on hangame.co.jp.", "Captcha..", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                            else
                            {
                                MessageBox.Show(message, "Couldn't authenticate, hangame says..", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                            }
                            return;
                        }
                    }
                }

                // 2: Get game start response & arguments
                var gameIni = new IniFile();
                gameIni.Load(Path.Combine(textBoxGamePath.Text, "General.ini"));

                var finished = false;
                while (!finished && proxyManager.Proxies.Count > 0)
                {
                    var proxy = await proxyManager.GetFirstWorkingProxy();

                    try
                    {
                        using (var httpClient = new HttpClient(new HttpClientHandler
                        {
                            CookieContainer = cookieContainer,
                            Proxy = proxy
                        }))
                        {
                            UpdateStatus("Requesting game start data");

                            var gameStartResponse = await httpClient.GetStringAsync("http://soulworker.hangame.co.jp/gamestart.nhn");
                            if (gameStartResponse.Contains("/common/ipCountry/validationFail.jsp"))
                            {
                                MessageBox.Show("You failed on the country check, try again.", "Validation fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                return;
                            }

                            var errorCode = gameStartResponse.ParseRegex("var errCode = \"(?<value>[0-9]+)\";");
                            if (errorCode == "03")
                            {
                                MessageBox.Show("You have to accept the hangame TOS.", "Validation fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                return;
                            }

                            var openCloseTypeCd = gameStartResponse.ParseRegex("var openCloseTypeCd = \"(?<value>[a-zA-Z]+)\";");
                            if (openCloseTypeCd == "C")
                            {
                                MessageBox.Show("SoulWorker is currently under maintenance.", "Validation fail", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                                return;
                            }

                            var gameStartArgs = new[]
                            {
                                gameStartResponse.ParseRegex("reactorStr = \"(?<value>.{1,}?)\""),
                                gameIni.Sections["Network Info"].Keys["IP"].Value,
                                gameIni.Sections["Network Info"].Keys["PORT"].Value
                            };
                            
                            var startInfo = new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                Verb = "runas",
                                Arguments = string.Join(" ", gameStartArgs.Select(s => $"\"{s}\"")),
                                WorkingDirectory = textBoxGamePath.Text,
                                FileName = "SoulWorker100.exe"
                            };

                            Process.Start(startInfo);

                            finished = true;
                        }
                    }
                    catch (HttpRequestException)
                    {
                        proxyManager.RemoveProxy(proxy);
                    }
                }

                UpdateStatus("Idle");
            }
        }

        internal void UpdateStatus(string status)
        {
            StatusLabel.Text = $"Status: {status}";
        }

        private void trackBarUptime_Scroll(object sender, EventArgs e)
        {
            lblProxyUptime.Text = $"Minimal proxy uptime: {trackBarUptime.Value}";
        }
    }
}
