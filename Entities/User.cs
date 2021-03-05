namespace QuaverBot.Entities
{
    public class User
    {
        public string Name { get; set; }
        public ulong Id { get; set; }
        public string QuaverId { get; set; }
        public GameMode PreferredMode { get; set; }
    }

    public enum GameMode
    {
        Key4,
        Key7
    }
}