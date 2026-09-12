namespace Tether.enums;

public static class EErrorMessageReturn
{
    // Token expired, application gonna check for new one. 
    public const string TOKEN_EXPIRED =  "Token expired";
    
    // Disconnected by server
    public const string TRANSPORT_ERROR = "transport error";
}
