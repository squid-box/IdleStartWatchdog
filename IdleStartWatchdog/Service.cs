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
        private ServiceStates _currentState;
        private DateTime _lastTimeUserLoggedOut;
        private bool _shutdownInitiated;

        public Service()
        {
            InitializeComponent();

            _log = new EventLog("Application", ".", "IdleStartWatchdog");
            _timer = new Timer(1000) {AutoReset = true};
            _timer.Elapsed += CheckStatus;
        }

        private void CheckStatus(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_currentState == ServiceStates.UserLoggedIn)
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
                    Log("User logged in.");
                    _currentState = ServiceStates.UserLoggedIn;
                }
                else
                {
                    var loggedOut = DateTime.Now - _lastTimeUserLoggedOut;

                    if (loggedOut > Watchdog.Default.TimeToWait.Add(Watchdog.Default.TimeToWait))
                    {
                        Log($"No-one has signed in after double the timeout, somethings wrong (hybrid boot?). Resetting timer.", EventLogEntryType.Error);
                        _lastTimeUserLoggedOut = DateTime.Now;
                    }
                    else if (loggedOut > Watchdog.Default.TimeToWait)
                    {
                        if (!_shutdownInitiated)
                        {
                            Log($"No-one has signed in after {Watchdog.Default.TimeToWait.ToString()}, shutting down computer.\nLast time someone signed out: {_lastTimeUserLoggedOut}", EventLogEntryType.Warning);
                            ShutdownComputer();
                        }                        
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            Initialize();
            _timer.Start();
        }

        protected override void OnStop()
        {
            _timer.Stop();
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            if (powerStatus == PowerBroadcastStatus.ResumeSuspend)
            {
                Initialize();
            }

            return base.OnPowerEvent(powerStatus);
        }

        private void Initialize()
        {
            _currentState = IsSomeoneLoggedOn ? ServiceStates.UserLoggedIn : ServiceStates.UserLoggedOut;
            _lastTimeUserLoggedOut = DateTime.Now;
            _shutdownInitiated = false;
        }

        private void ShutdownComputer()
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "shutdown",
                Arguments = "/s /t 15",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            _shutdownInitiated = true;
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
