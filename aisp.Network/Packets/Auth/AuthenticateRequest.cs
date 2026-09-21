using aisp.Network;

namespace aisp.Network.Packets.Auth;

public class AuthenticateRequest(string username, string password)
    : IIncomingPacket<AuthenticateRequest>
{
    public string Username = username;
    public string Password = password;

    // 2008 extra=2 auth proto writes a third CString; 2009 sends two.
    public string Extra = "";

    public static AuthenticateRequest FromBytes(ReadOnlySpan<byte> data)
    {
        PacketReader reader = new(data);

        string username = reader.ReadString();
        string password = reader.ReadString();
        var req = new AuthenticateRequest(username, password);
        if (reader.Remaining > 0)
            req.Extra = reader.ReadString();
        return req;
    }
}
