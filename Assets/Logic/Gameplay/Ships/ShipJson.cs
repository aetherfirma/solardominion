using System;

namespace Logic.Gameplay.Ships
{
    [Serializable]
    public class ShipJson
    {
        public string Uuid;
        public string ShipUuid;
        public int Training;

        public ShipJson(string uuid, int training, string shipUuid)
        {
            Uuid = uuid;
            Training = training;
            ShipUuid = shipUuid;
        }
    }
}