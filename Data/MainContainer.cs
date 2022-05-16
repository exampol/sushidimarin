namespace BlazorApp.Data
{
    public static class MainContainer
    {
        public static Dictionary<string, Dictionary<string, int>> rooms = new Dictionary<string, Dictionary<string, int>>();

        public static Dictionary<string, int> GetOrders(string room)
        {
            string realRoom = room.ToUpperInvariant();
            if (!rooms.ContainsKey(realRoom)) rooms.Add(realRoom, new Dictionary<string, int>());

            return rooms[realRoom];
        }
        public static void AddOrder(string room, string item, int iQty)
        {
            string realRoom = room.ToUpperInvariant();
            Dictionary<string, int> orders = GetOrders(realRoom);

            if(!orders.ContainsKey(item)) orders.Add(item, iQty);
            else orders[item] += iQty;
        }
    }
}
