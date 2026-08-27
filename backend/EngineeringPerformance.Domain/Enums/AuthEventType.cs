namespace EngineeringPerformance.Domain.Enums;

public enum AuthEventType
{
    LoginSuccess = 100,
    LoginFailed = 101,
    LoginLockedOut = 102,
    NegotiateSuccess = 200,
    NegotiateFailed = 201,
    NegotiateDisabled = 202,
    RefreshSuccess = 300,
    RefreshFailed = 301,
    RefreshRevoked = 302,
    Logout = 400,
    LogoutAllDevices = 401,
    MfaSetupStarted = 500,
    MfaSetupCompleted = 501,
    MfaVerifySuccess = 502,
    MfaVerifyFailed = 503,
    MfaReset = 504,
    MfaDisabled = 505,
    PasswordChanged = 600,
    PasswordResetByAdmin = 601,
    AccountUnlocked = 602,
    UserCreated = 603,
    UserDeactivated = 604,
    RoleChanged = 605,
    AdSyncCompleted = 700,
    AdSyncFailed = 701
}
