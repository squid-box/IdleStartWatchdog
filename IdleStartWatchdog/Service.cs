namespace IdleStartWatchdog
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.ServiceProcess;
    using System.Timers;

    using Cassia;

    public partial class Service : ServiceBase
    {
        private readonly Timer _timer;
        private const int MinutesToWaitBeforeShutdown = 5;

        public Service()
        {
            InitializeComponent();
            _timer = new Timer(6000) {AutoReset = true};
            _timer.Elapsed += CheckStatus;
            Log("Service initialized.");
        }

        private void CheckStatus(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            // If someone has logged in, this service is done.
            if (IsSomeoneLoggedOn())
            {
                Log("Someone logged on, stopping service.");
                this.Stop();
            }
            else if (TimeSpan.FromMilliseconds(Environment.TickCount) > TimeSpan.FromMinutes(MinutesToWaitBeforeShutdown))
            {
                // Check if computer has been running for long enough to shut it down.
                Log($"Noone logged on after {MinutesToWaitBeforeShutdown} minutes, shutting down computer.");
                ShutdownComputer();
            }
            else
            {
                // Just keep rolling...
                //Log($"Time since computer started: {TimeSpan.FromMilliseconds(Environment.TickCount)}, time limit: {TimeSpan.FromMinutes(MinutesToWaitBeforeShutdown)}.");
            }
        }

        protected override void OnStart(string[] args)
        {
            Log("Starting timer/service.");
            _timer.Start();
        }

        protected override void OnStop()
        {
            Log("Stopping timer/service.");
            _timer.Stop();
        }

        private static void ShutdownComputer()
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "shutdown",
                Arguments = "/s /t 0",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(psi);
        }

        /// <summary>
        /// http://stackoverflow.com/a/732639
        /// </summary>
        /// <returns>True if any user is logged in.</returns>
        private static bool IsSomeoneLoggedOn()
        {
            foreach (var session in new TerminalServicesManager().GetSessions())
            {
                if (!string.IsNullOrEmpty(session.UserName))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Log(string message)
        {
            File.AppendAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\service.log", $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}|{message}\r\n");
        }
    }
}
