using AmenoLink.Configurations;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AmenoLink.Hubs;

internal partial class MainHub
{
    private string StoreChannel(string name) => $"store:{name}";
}
