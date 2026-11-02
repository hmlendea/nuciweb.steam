namespace NuciWeb.Steam
{
    /// <summary>
    /// Key activation error code.
    /// </summary>
    public enum KeyActivationErrorCode
    {
        Unexpected = 0,
        InvalidProductKey = 1,
        AlreadyActivatedDifferentAccount = 2,
        AlreadyActivatedCurrentAccount = 3,
        BaseProductRequired = 4,
        RegionLocked = 5,
        TooManyAttempts = 6
    }
}
