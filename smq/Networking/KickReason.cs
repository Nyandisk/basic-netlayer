namespace Vikinet2.Networking {
    /// <summary>
    /// Enum for the reasons a client can be kicked from the server
    /// Feel free to expand this enum for your own needs
    /// </summary>
    public enum KickReason {
        ServerFull = 0x0001,
        QuestionableActivity = 0x0002
    }
}
