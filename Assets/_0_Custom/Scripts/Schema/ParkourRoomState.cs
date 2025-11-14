namespace ParkourLegion.Schema
{
    public class ParkourRoomState : Colyseus.Schema.Schema
    {
        [Colyseus.Schema.Type(0, "map", typeof(Colyseus.Schema.MapSchema<PlayerState>))]
        public Colyseus.Schema.MapSchema<PlayerState> players = new Colyseus.Schema.MapSchema<PlayerState>();

        [Colyseus.Schema.Type(1, "float32")]
        public float raceStartTime = 0;

        [Colyseus.Schema.Type(2, "boolean")]
        public bool raceStarted = false;

        [Colyseus.Schema.Type(3, "string")]
        public string gameState = "waiting";

        [Colyseus.Schema.Type(4, "uint8")]
        public byte countdownValue = 0;

        [Colyseus.Schema.Type(5, "uint8")]
        public byte playerCount = 0;
    }
}
