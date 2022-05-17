using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorApp.Data
{
  public static class MainContainer
  {
    [Inject]
    public static NavigationManager NavigationManager { get; set; }

    public static Dictionary<string, DateTime> roomDates = new Dictionary<string, DateTime>();
    public static Dictionary<string, Dictionary<string, int>> rooms = new Dictionary<string, Dictionary<string, int>>();
    public static Dictionary<string, HubConnection?> connections = new Dictionary<string, HubConnection?>();
    public static Dictionary<string, int> GetOrders(string room)
    {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      SetModRoom(realRoom);
      CleanRooms();

      return rooms[realRoom];
    }
    public static async Task AddOrderAsync(string room, string item, int iQty)
    {
      string realRoom = room.ToUpperInvariant();
      Dictionary<string, int> orders = GetOrders(realRoom);

      if (!orders.ContainsKey(item)) orders.Add(item, iQty);
      else orders[item] += iQty;

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null)
      {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static async Task RemoveOrderAsync(string room, string item, int iQty)
    {
      string realRoom = room.ToUpperInvariant();
      Dictionary<string, int> orders = GetOrders(realRoom);

      if (iQty == -1) orders.Remove(item);
      else
      {
        if (!orders.ContainsKey(item)) return;
        orders[item] -= iQty;
        if (orders[item] <= 0) orders.Remove(item);
      }

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null)
      {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static async Task RemoveAllOrdersAsync(string room)
    {
      string realRoom = room.ToUpperInvariant();
      Dictionary<string, int> orders = GetOrders(realRoom);
      orders.Clear();

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null)
      {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static void CleanRooms()
    {
      DateTime now = DateTime.Now;
      for (int i = 0; i < roomDates.Count; i++)
      {
        string room = roomDates.Keys.ElementAt(i);
        DateTime lastMod = roomDates[room];

        if (lastMod.AddDays(3) < now)
        {
          rooms.Remove(room);
          roomDates.Remove(room);
          connections.Remove(room);
          i--;
        }
      }
    }

    public static void SetModRoom(string room)
    {
      string realRoom = room.ToUpperInvariant();
      if (roomDates.ContainsKey(realRoom)) roomDates[realRoom] = DateTime.Now;
    }

    public static void CreateRoom(string room)
    {
      string realRoom = room.ToUpperInvariant();
      if (!rooms.ContainsKey(realRoom)) rooms.Add(realRoom, new Dictionary<string, int>());
      if (!roomDates.ContainsKey(realRoom)) roomDates.Add(realRoom, DateTime.Now);
    }
  }
}
