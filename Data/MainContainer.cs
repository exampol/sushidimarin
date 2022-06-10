using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorApp.Data
{
  public class Room
  {
    // Dizionario oggetto - (Dizionario proprietario - quantità)
    public Dictionary<string, Dictionary<string, int>> items = new Dictionary<string, Dictionary<string, int>>();
    public DateTime LastMod = DateTime.MinValue;

    public Room() { LastMod = DateTime.Now; }
    private void CleanItems()
    {
      for (int j = 0; j < items.Count; j++)
      {
        string item = items.ElementAt(j).Key;
        for (int i = 0; i < items[item].Count; i++)
        {
          KeyValuePair<string, int> keyValuePair = items[item].ElementAt(i);
          if (keyValuePair.Value <= 0) { items[item].Remove(keyValuePair.Key); i--; }
        }
      }
    }

    public Dictionary<string, int> GetByOwner(string owner)
    {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      for (int i = 0; i < items.Count; i++)
      {
        string item = items.ElementAt(i).Key;
        Dictionary<string, int> clients = items.ElementAt(i).Value;
        if (!items.ElementAt(i).Value.ContainsKey(owner)) continue;
        ret.Add(item, items.ElementAt(i).Value[owner]);
      }
      LastMod = DateTime.Now;
      return ret;
    }

    public Dictionary<string, int> GetByItem(string item)
    {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      if (!items.ContainsKey(item)) return ret;

      for (int i = 0; i < items[item].Count; i++)
      {
        KeyValuePair<string, int> keyValuePair = items[item].ElementAt(i);
        ret.Add(keyValuePair.Key, keyValuePair.Value);
      }
      LastMod = DateTime.Now;
      return ret;
    }

    public Dictionary<string, int> GetSummary()
    {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      for (int i = 0; i < items.Count; i++)
      {
        int count = items.ElementAt(i).Value.Select(x => x.Value).Sum();
        ret.Add(items.ElementAt(i).Key, count);
      }
      LastMod = DateTime.Now;
      return ret;
    }

    public void EditOrder(string item, string owner, int qty)
    {
      if (!items.ContainsKey(item)) items.Add(item, new Dictionary<string, int>());
      if (!items[item].ContainsKey(owner)) items[item].Add(owner, Math.Max(qty, 0));
      else items[item][owner] += qty;

      LastMod = DateTime.Now;
      CleanItems();
    }
    public void RemoveOrder(string item, string owner)
    {
      if (!items.ContainsKey(item)) items.Add(item, new Dictionary<string, int>());
      items[item].Remove(owner);

      LastMod = DateTime.Now;
      CleanItems();
    }

    public void Clear() { items.Clear(); LastMod = DateTime.Now; }
  }

  public static class MainContainer
  {
    [Inject]
    public static NavigationManager NavigationManager { get; set; }

    public static Dictionary<string, Room> rooms = new Dictionary<string, Room>();
    public static Dictionary<string, HubConnection?> connections = new Dictionary<string, HubConnection?>();
    public static Dictionary<string, int> GetOrders(string room)
    {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetSummary();
    }
    public static Dictionary<string, int> GetDetails(string room, string item)
    {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetByItem(item);
    }
    public static async Task AddOrderAsync(string room, string item, string owner, int iQty)
    {
      string realRoom = room.ToUpperInvariant();
      if (rooms.ContainsKey(realRoom)) rooms[realRoom].EditOrder(item, owner, Math.Max(iQty, 0));

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

    public static async Task RemoveOrderAsync(string room, string item, string owner, int iQty)
    {
      string realRoom = room.ToUpperInvariant();
      if (rooms.ContainsKey(realRoom)) { 
        if (iQty == -1) rooms[realRoom].RemoveOrder(item, owner); 
        else rooms[realRoom].EditOrder(item, owner, -Math.Max(iQty, 0));
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
      if (rooms.ContainsKey(realRoom)) rooms[realRoom].Clear();

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
      for (int i = 0; i < rooms.Count; i++)
      {
        string room = rooms.Keys.ElementAt(i);
        DateTime lastMod = rooms[room].LastMod;

        if (lastMod.AddDays(3) < now)
        {
          rooms.Remove(room);
          connections.Remove(room);
          i--;
        }
      }
    }

    public static void CreateRoom(string room)
    {
      string realRoom = room.ToUpperInvariant();
      if (!rooms.ContainsKey(realRoom)) rooms.Add(realRoom, new Room());
    }
  }
}
