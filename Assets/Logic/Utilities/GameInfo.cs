namespace Logic.Utilities
{
    public struct GameInfo
    {
        public string Name, Username, Scenario;
        public int PointsLimit, Created;

        public GameInfo(string name, string username, string scenario, int pointsLimit, int created)
        {
            Name = name;
            Username = username;
            Scenario = scenario;
            PointsLimit = pointsLimit;
            Created = created;
        }
    }
}