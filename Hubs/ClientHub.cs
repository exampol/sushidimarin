using Microsoft.AspNetCore.SignalR;

namespace BlazorApp.Hubs
{
  public class ClientHub : Hub
  {
    public async Task SendMessage(string room, string message)
    {
      await Clients.All.SendAsync("ReceiveMessage", room, message);
    }
  }
}
