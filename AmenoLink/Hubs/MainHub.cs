using AmenoLink.Interfaces.Hub;
using Microsoft.AspNetCore.SignalR;

namespace AmenoLink.Hubs;

internal partial class MainHub : Hub, IMainHub
{
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var client = new ConnectedClient { ConnectionId = connectionId };
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        await base.OnDisconnectedAsync(exception);
    }
}
