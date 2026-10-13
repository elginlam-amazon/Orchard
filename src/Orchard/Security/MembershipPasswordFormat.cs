namespace Orchard.Security
{
    /// <summary>
    /// The storage format of the passwords (same values as the former System.Web.Security.MembershipPasswordFormat,
    /// which are persisted in the database).
    /// </summary>
    public enum MembershipPasswordFormat
    {
        Clear = 0,
        Hashed = 1,
        Encrypted = 2
    }

    /// <summary>
    /// The result of a user creation (same values as the former System.Web.Security.MembershipCreateStatus).
    /// </summary>
    public enum MembershipCreateStatus
    {
        Success = 0,
        InvalidUserName = 1,
        InvalidPassword = 2,
        InvalidQuestion = 3,
        InvalidAnswer = 4,
        InvalidEmail = 5,
        DuplicateUserName = 6,
        DuplicateEmail = 7,
        UserRejected = 8,
        InvalidProviderUserKey = 9,
        DuplicateProviderUserKey = 10,
        ProviderError = 11
    }
}
