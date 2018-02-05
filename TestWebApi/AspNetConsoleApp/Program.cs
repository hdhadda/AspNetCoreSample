// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
//   Defines the Program type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.ManagementExperience.Desktop
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.AspNetCore.Hosting;
    using Win32;

    /// <summary>
    /// The Program
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// used to name mutex to run single instance of application
        /// </summary>
        private const string ApplicationGuid = "28FD29F3-0737-48DB-8442-6DC4B86BC878";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Mutex mutex = null;
            try
            {
                mutex = new Mutex(false, ApplicationGuid);
                if (mutex.WaitOne(0, false))
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    using (var gatewayIcon = new GatewayIcon())
                    {
                        gatewayIcon.Show();
                        IWebHost host = TestWebApi.HostEntry.BuildWebHost(null); 
                        var hostTask = host.RunAsync(); // Run();
                        // Process.Start("http://localhost:5050/api/values/6");

                        Task.Run(() =>
                        {
                            while (true)
                            {
                                // Startup.RestartEvent.WaitOne();
                                // Startup.RestartEvent.Reset();
                                Thread.Sleep(200);
                                // Startup.Stop();
                                // Startup.Start(GatewayMode.Desktop);
                            }
                        });

                        Task.Run(() =>
                        {
                            // FrontEnd.Startup.ElevateEvent.WaitOne();
                            Elevate();
                        });

                        // create an invisible form to process windows messages, start application using ApplicationContext so as to not
                        // display the form, used by windows installer to notify application to exit prior to uninstall
                        var form = new Form();
                        form.Load += (_, __) =>
                        {
                            form.Visible = false;
                            form.ShowInTaskbar = false;
                        };
                        Application.Run(new ApplicationContext(form));
                        hostTask.Wait();
                    }
                }
                else
                {
                    var endpoint = GetDesktopEndpoint();
                    if (!string.IsNullOrEmpty(endpoint) && IsGatewayApplicationRunning(endpoint))
                    {
                        Process.Start(endpoint);
                    }
                    else
                    {
                        MessageBox.Show("Resource.InstanceAlreadyRun");
                    }
                }
            }
            finally
            {
                mutex?.Close();
            }
        }

        /// <summary>
        /// The get desktop endpoint.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetDesktopEndpoint()
        {
            using (var subKey = Registry.CurrentUser.OpenSubKey("Constants.SmeKeyName"))
            {
                return (string)subKey?.GetValue("Constants.SmeFrontendEndpointRegKeyName");
            }
        }

        /// <summary>
        /// check if the gateway app is running
        /// </summary>
        /// <param name="endpoint">endpoint gateway app is running on</param>
        /// <returns>true is app is listening on endpoint</returns>
        private static bool IsGatewayApplicationRunning(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return false;
            }

            using (var client = new TcpClient())
            {
                try
                {
                    Uri uri = new Uri(endpoint);
                    client.Connect(uri.Host, uri.Port);
                    return true;
                }
                catch (SocketException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Restart process
        /// </summary>
        private static void Restart()
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = "/C ping 127.0.0.1 -n 2 && \"" + Application.ExecutablePath + "\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "cmd.exe"
            };

            Process.Start(startInfo);
            Application.Exit();
        }

        /// <summary>
        /// Restart process with elevated privilege
        /// </summary>
        private static void Elevate()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    Arguments = "/C ping 127.0.0.1 -n 2 && \"" + Application.ExecutablePath + "\"",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    FileName = "cmd.exe",
                    Verb = "runas"
                };

                Process.Start(startInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Elevation failed. Message - {ex.Message}, StackTrace - {ex.StackTrace}");
            }
        }
    }
}
