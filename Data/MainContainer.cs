using Microsoft.AspNetCore.Connections.Features;

namespace BlazorApp.Data
{
  public static class MainContainer
  {
    public static Dictionary<string, DateTime> roomDates = new Dictionary<string, DateTime>();
    public static Dictionary<string, Dictionary<string, int>> rooms = new Dictionary<string, Dictionary<string, int>>();
    public static Dictionary<string, List<string>> users = new Dictionary<string, List<string>>();
    public static Dictionary<string, Tuple<Action, DateTime>> callBacks = new Dictionary<string, Tuple<Action, DateTime>>();
    public static Dictionary<string, int> GetOrders(string room)
    {
      string realRoom = room.ToUpperInvariant();
      CreateRoom(realRoom);
      SetModRoom(realRoom);
      CleanRooms();

      return rooms[realRoom];
    }
    public static void AddOrder(string room, string item, int iQty)
    {
      string realRoom = room.ToUpperInvariant();
      Dictionary<string, int> orders = GetOrders(realRoom);

      if (!orders.ContainsKey(item)) orders.Add(item, iQty);
      else orders[item] += iQty;

      List<Action> actions = GetCallBacks(room);
      Console.WriteLine("Chiamo le callback: " + actions.Count);
      for (int i = 0; i < actions.Count; i++)
      {
        try { actions[i].Invoke(); } catch (Exception) { }
      }
    }

    private static Action? GetCallBack(string room, string user)
    {
      string realRoom = room.ToUpperInvariant();
      if (!users.ContainsKey(realRoom)) return null;
      if (!callBacks.ContainsKey(user)) return null;

      List<string> lstUsers = users[realRoom];
      for (int i = 0; i < lstUsers.Count; i++)
      {
        if (string.Equals(lstUsers[i], user)) return callBacks[user].Item1;
      }
      return null;
    }

    private static List<Action> GetCallBacks(string room)
    {
      List<Action> actions = new List<Action>();
      string realRoom = room.ToUpperInvariant();
      if (!users.ContainsKey(realRoom)) return actions;

      List<string> lstUsers = users[realRoom];
      for (int i = 0; i < lstUsers.Count; i++) { if (callBacks.ContainsKey(lstUsers[i])) actions.Add(callBacks[lstUsers[i]].Item1); }
      return actions;
    }

    public static void AddCallback(string room, string user, Action action)
    {
      string realRoom = room.ToUpperInvariant();
      if (!users.ContainsKey(realRoom)) return;

      List<string> lstUsers = users[realRoom];

      bool bFound = false;
      for (int i = 0; i < lstUsers.Count; i++) { if (lstUsers[i] == user) { bFound = true; break; } }
      if (!bFound) users[realRoom].Add(user);

      Console.WriteLine("Aggiungo callback");
      if (!callBacks.ContainsKey(user)) callBacks.Add(user, new Tuple<Action, DateTime>(action, DateTime.Now));
      else callBacks[user] = new Tuple<Action, DateTime>(action, DateTime.Now);
      Console.WriteLine("Aggiunta callback!");
    }

    public static void RemoveOrder(string room, string item, int iQty)
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
    }

    public static void RemoveAllOrders(string room)
    {
      string realRoom = room.ToUpperInvariant();
      Dictionary<string, int> orders = GetOrders(realRoom);
      orders.Clear();
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
          users.Remove(room);
          i--;
        }
      }

      for (int i = 0; i < callBacks.Count; i++)
      {
        string user = callBacks.Keys.ElementAt(i);
        DateTime lastMod = callBacks[user].Item2;
        if (lastMod.AddDays(3) < now)
        {
          callBacks.Remove(user);
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
      if (!users.ContainsKey(realRoom)) users.Add(realRoom, new List<string>());
    }
  }
}
