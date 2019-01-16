# Idle-Start Watchdog

This is a counter-measure to my cats starting my desktop computer by walking on the case during the night, but not shutting it down afterwards (rude).

This is a Windows Service that is started when the system is booted, it will shut down the computer if :
* it has been started and running for a set amount of time, but no-one has logged in.
* a user has logged out, but no-one has logged in for a set amount of time.

This program uses [Cassia](https://github.com/ng-pe/cassia) (through NuGet).
