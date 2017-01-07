namespace IdleStartWatchdog
{
    /// <summary>
    /// Different states available to the service.
    /// </summary>
    public enum ServiceStates
    {
        /// <summary>
        /// Computer has started, but noone has logged in.
        /// </summary>
        InitialStart,

        /// <summary>
        /// User is currently logged in.
        /// </summary>
        UserLoggedIn,

        /// <summary>
        /// User has been logged in, but is now logged out again.
        /// </summary>
        UserLoggedOut
    }
}
