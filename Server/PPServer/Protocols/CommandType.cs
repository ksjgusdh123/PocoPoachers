namespace PPServer.Protocols;

public enum CommandType : byte
{
    None = 0,
    Login = 1,
    Chat = 2,
    Logout = 3,
}
