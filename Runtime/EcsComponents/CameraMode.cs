namespace StormiumTeam.GameBase
{
    public enum CameraMode
    {
        /// <summary>
        /// The camera will not be ruled by this state and will revert to Default mode if there are
        /// no other states with '<see cref="Forced"/>' mode.
        /// </summary>
        Default = 0,
        /// <summary>
        /// The camera will be forced to the rules of this state and override previous states.
        /// </summary>
        Forced = 1
    }
}