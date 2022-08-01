using System.Reflection.Metadata;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorApp.Data {

  public class Items {
    private DateTime? revisionDate = null;
    public DateTime? RevisionDate => revisionDate;

    private Dictionary<string, Dictionary<string, int>> items = new Dictionary<string, Dictionary<string, int>>();

    private bool bEditable = true;
    public void MakeReadOnly() { revisionDate = DateTime.Now; bEditable = false; }

    public Items() { bEditable = true; }
    public Items(Items old) {
      for (int i = 0; i < old.items.Count; i++) {
        string item  = old.items.ElementAt(i).Key;
        items.Add(item, new Dictionary<string, int>());
        for (int j = 0; j < old.items[item].Count; j++) {
          string owner = old.items[item].ElementAt(j).Key;
          items[item].Add(owner, old.items[item][owner]);
        }
      }
      this.revisionDate = old.revisionDate;
      this.bEditable = old.bEditable;
    }

    public int GetItemCount() { return items.Count; }
    public string GetItem(int index) { return items.Keys.Count > index ? items.Keys.ElementAt(index) : null; }
    public int GetOrdersCount(string item) { return items.ContainsKey(item) ? items[item].Count : -1; }
    public string GetOrderOwner(string item, int owner) { return items.ContainsKey(item) && items[item].Count > owner ? items[item].Keys.ElementAt(owner) : null; }
    public int GetOrderQuantity(string item, int owner) { return items.ContainsKey(item) && items[item].Count > owner ? items[item].Values.ElementAt(owner) : -1; }
    public int GetOrderQuantity(string item, string owner) { return items.ContainsKey(item) && items[item].ContainsKey(owner) ? items[item][owner] : -1; }

    public bool ContainsItem(string item) { return items.ContainsKey(item); }
    public bool ContainsOwner(string item, string owner) { return items[item].ContainsKey(owner); }

    public int GetSummary(string item) { return items[item].Select(x => x.Value).Sum(); }

    public void EditQuantity(int item, int owner, int qty) {
      if (!bEditable) return;
      string sitem = GetItem(item);
      if (string.IsNullOrWhiteSpace(sitem)) return;
      string sowner = GetOrderOwner(sitem, owner);
      if (string.IsNullOrWhiteSpace(sowner)) return;
      EditQuantity(sitem, sowner, qty);
    }
    public void EditQuantity(string item, string owner, int qty) {
      if (!bEditable) return;
      if (!items.ContainsKey(item)) items.Add(item, new Dictionary<string, int>());
      if (!items[item].ContainsKey(owner)) items[item].Add(owner, Math.Max(qty, 0));
      else items[item][owner] += qty;
    }

    public void RemoveOrder(string item, string owner) {
      if (!bEditable) return;
      if (!items.ContainsKey(item)) return;
      if (!items[item].ContainsKey(owner)) return;
      items[item].Remove(owner);
    }
    public Dictionary<string, int> GetByOwner(string owner) {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      for (int i = 0; i < GetItemCount(); i++) {
        string item = GetItem(i);
        if (string.IsNullOrWhiteSpace(item)) continue;
        int qty = GetOrderQuantity(item, owner);
        if (qty == -1) continue;
        ret.Add(item, qty);
      }
      return ret;
    }

    public Dictionary<string, int> GetByItem(string item) {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      if (!ContainsItem(item)) return ret;

      for (int i = 0; i < GetOrdersCount(item); i++) {
        string owner = GetOrderOwner(item, i);
        int qty = GetOrderQuantity(item, i);
        if (qty == -1) continue;
        ret.Add(owner, qty);
      }
      return ret;
    }

    public Dictionary<string, int> GetSummary() {
      Dictionary<string, int> ret = new Dictionary<string, int>();
      for (int i = 0; i < GetItemCount(); i++) {
        string item = GetItem(i);
        int count = GetSummary(item);
        ret.Add(item, count);
      }
      return ret;
    }

    public void Clean() {
      if (!bEditable) return;
      for (int j = 0; j < items.Count; j++) {
        string item = items.ElementAt(j).Key;
        if (items[item].Count == 0) {
          items.Remove(item);
        } else {
          for (int i = 0; i < items[item].Count; i++) {
            KeyValuePair<string, int> keyValuePair = items[item].ElementAt(i);
            if (keyValuePair.Value <= 0) { items[item].Remove(keyValuePair.Key); i--; }
          }
        }
      }
    }

    public void Clear() { if (!bEditable) return; items.Clear(); }
  }

  public class Room {
    // Dizionario oggetto - (Dizionario proprietario - quantità)
    public Items items = new Items();
    public List<Items> oldItems = new List<Items>();
    public DateTime LastMod = DateTime.MinValue;

    public Room() { LastMod = DateTime.Now; }

    public Dictionary<string, int> GetByOwner(string owner, int iOldVersion = -1) {
      Dictionary<string, int> ret = null;
      if (iOldVersion == -1) ret = items.GetByOwner(owner);
      else ret = oldItems[iOldVersion].GetByOwner(owner);
      LastMod = DateTime.Now;
      return ret;
    }
    public Dictionary<string, int> GetByItem(string item, int iOldVersion = -1) {
      Dictionary<string, int> dictionary = null;
      if (iOldVersion == -1) dictionary = items.GetByItem(item);
      else dictionary = oldItems[iOldVersion].GetByItem(item);
      LastMod = DateTime.Now;
      return dictionary;
    }

    public Dictionary<string, int> GetSummary(int iOldVersion = -1) {
      Dictionary<string, int> dictionary = null;
      if (iOldVersion == -1) dictionary = items.GetSummary();
      else dictionary = oldItems[iOldVersion].GetSummary();
      LastMod = DateTime.Now;
      return dictionary;
    }

    public void EditOrder(string item, string owner, int qty) {
      items.EditQuantity(item, owner, qty);
      LastMod = DateTime.Now;
      items.Clean();
    }

    public void RemoveOrder(string item, string owner) {
      items.RemoveOrder(item, owner);
      LastMod = DateTime.Now;
      items.Clean();
    }

    public int GetOldVersionsCount() { return oldItems.Count; }
    public Items GetOldVersion(int index) { return oldItems[index]; }

    public void Clear() { Items copy = new Items(items); copy.MakeReadOnly(); oldItems.Add(copy); items.Clear(); LastMod = DateTime.Now; }
  }

  public static class MainContainer {
    [Inject]
    public static NavigationManager NavigationManager { get; set; }

    public static Dictionary<string, Room> rooms = new Dictionary<string, Room>();
    public static Dictionary<string, HubConnection?> connections = new Dictionary<string, HubConnection?>();
    public static Dictionary<string, int> GetOrders(string room, int iOldVersion = -1) {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetSummary(iOldVersion);
    }
    public static Dictionary<string, int> GetDetails(string room, string item, int iOldVersion = -1) {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetByItem(item, iOldVersion);
    }

    public static int GetOldVersionsCount(string room) {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetOldVersionsCount();
    }

    public static async Task AddOrderAsync(string room, string item, string owner, int iQty) {
      string realRoom = room.ToUpperInvariant();
      if (rooms.ContainsKey(realRoom)) rooms[realRoom].EditOrder(item, owner, Math.Max(iQty, 0));

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null) {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static async Task RemoveOrderAsync(string room, string item, string owner, int iQty) {
      string realRoom = room.ToUpperInvariant();
      if (rooms.ContainsKey(realRoom)) {
        if (iQty == -1) rooms[realRoom].RemoveOrder(item, owner);
        else rooms[realRoom].EditOrder(item, owner, -Math.Max(iQty, 0));
      }

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null) {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static async Task RemoveAllOrdersAsync(string room) {
      string realRoom = room.ToUpperInvariant();
      if (rooms.ContainsKey(realRoom)) rooms[realRoom].Clear();

      if (!connections.ContainsKey(realRoom) || connections[realRoom] is null) {
        Uri uri = NavigationManager.ToAbsoluteUri("/clienthub");
        HubConnection hubConnection = new HubConnectionBuilder().WithUrl(uri).Build();
        await hubConnection.StartAsync();
        if (!connections.ContainsKey(realRoom)) connections.Add(realRoom, hubConnection);
        else connections[realRoom] = hubConnection;
      }

      await connections[realRoom].SendAsync("SendMessage", room, "update");
    }

    public static Dictionary<string, int> GetByOwner(string room, string owner, int iOldVersion = -1) {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      CleanRooms();

      return rooms[realRoom].GetByOwner(owner, iOldVersion);
    }

    public static void CleanRooms() {
      DateTime now = DateTime.Now;
      for (int i = 0; i < rooms.Count; i++) {
        string room = rooms.Keys.ElementAt(i);
        DateTime lastMod = rooms[room].LastMod;

        if (lastMod.AddDays(3) < now) {
          rooms.Remove(room);
          connections.Remove(room);
          i--;
        }
      }
    }

    public static void CreateRoom(string room) {
      string realRoom = room.ToUpperInvariant();
      if (!rooms.ContainsKey(realRoom)) rooms.Add(realRoom, new Room());
    }
  }
}
