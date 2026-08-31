public static class ESocketEvent
{
    // Receiving a request from other player to start group chat
    public const string SEND_GROUP_REQUEST = "SEND_GROUP_REQUEST";
    
    // Receiving information that player accepted our group request - its either creating new group if chat doesn't exist, or joining existing one.
    public const string ACCEPT_GROUP_REQUEST = "ACCEPT_GROUP_REQUEST";
    
    // Main event of receiving info about getting message from group
    public const string SEND_GROUP_MESSAGE  = "SEND_GROUP_MESSAGE";
    
    // Getting information about request to join group
    public const string INVITE_TO_GROUP = "INVITE_TO_GROUP";
    
    // Bunch of updating requests of group - check ERoomupdateAction to see actions of it.
    public const string UPDATE_ROOM = "UPDATE_ROOM";

}
