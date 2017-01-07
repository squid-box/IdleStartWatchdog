namespace IdleStartWatchdog
{
    using System;
    using System.Diagnostics;
    using System.ServiceProcess;
    using System.Timers;

    using Cassia;

    public partial class Service : ServiceBase
    {
        private EventLog _log;
        private readonly Timer _timer;
        private const int MinutesToWaitBeforeShutdown = 5;
        private ServiceStates _currentState;
        private DateTime _lastTimeUserLoggedOut;

        public Service()
        {
            InitializeComponent();
            _currentState = ServiceStates.InitialStart;
            _lastTimeUserLoggedOut = DateTime.MinValue;

            _log = new EventLog("Application", ".", "IdleStartWatchdog");
            _timer = new Timer(1000) {AutoReset = true};
            _timer.Elapsed += CheckStatus;
        }

        private void CheckStatus(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_currentState == ServiceStates.InitialStart)
            {
                // If someone has logged in, this service is done.
                if (IsSomeoneLoggedOn)
                {
                    Log("User logged on, switching state.");
                    _currentState = ServiceStates.UserLoggedIn;
                }
                else if (TimeSpan.FromMilliseconds(Environment.TickCount) > TimeSpan.FromMinutes(MinutesToWaitBeforeShutdown))
                {
                    // Check if computer has been running for long enough to shut it down.
                    Log($"No-one logged on after {MinutesToWaitBeforeShutdown} minutes, shutting down computer.", EventLogEntryType.Warning);
                    ShutdownComputer();
                }
            }
            else if (_currentState == ServiceStates.UserLoggedIn)
            {
                if (!IsSomeoneLoggedOn)
                {
                    Log("User logged out.");
                    _currentState = ServiceStates.UserLoggedOut;
                    _lastTimeUserLoggedOut = DateTime.Now;
                }
            }
            else if (_currentState == ServiceStates.UserLoggedOut)
            {
                if (IsSomeoneLoggedOn)
                {
                    Log("Someone logged in again.");
                    _currentState = ServiceStates.UserLoggedIn;
                }
                else
                {
                    var loggedOut = DateTime.Now - _lastTimeUserLoggedOut;
                    if (loggedOut > TimeSpan.FromMinutes(MinutesToWaitBeforeShutdown))
                    {
                        Log($"No-one has signed in again after {MinutesToWaitBeforeShutdown} minutes, shutting down computer.", EventLogEntryType.Warning);
                        ShutdownComputer();
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            _timer.Start();
        }

        protected override void OnStop()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
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
        private static bool IsSomeoneLoggedOn
        {
            get
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
        }

        private void Log(string message, EventLogEntryType level = EventLogEntryType.Information)
        {
            _log.WriteEntry(message, level);
        }
    }
}
