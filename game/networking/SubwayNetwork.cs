namespace MmoGame3d.Networking;

using System;
using System.Diagnostics;
using Godot;

/// <summary>
/// The subway's visitor book: asking for a page, and the page. Spraying a name is an
/// ordinary use of the wall (ServerInteractions); the wall's tags are synced on the wall.
/// </summary>
public partial class SubwayNetwork : NetworkNode
{
    // Server side: (peer, page).
    public event Action<long, int>? PageRequested;

    // Client side: (page, pages, lines).
    public event Action<int, int, string[]>? PageReceived;

    public void SendReadBook(int page)
    {
        RpcId(1, MethodName.ReadBook, page);
    }

    public void SendPage(long peer, int page, int pages, string[] lines)
    {
        SendTo(peer, MethodName.ReceivePage, page, pages, lines);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReadBook(int page)
    {
        if (Multiplayer.IsServer())
        {
            long sender = Multiplayer.GetRemoteSenderId();

            using (Activity? span = Received(MethodName.ReadBook, sender, page))
            {
                PageRequested?.Invoke(sender, page);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ReceivePage(int page, int pages, string[] lines)
    {
        PageReceived?.Invoke(page, pages, lines);
    }
}
