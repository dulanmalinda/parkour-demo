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
    }
}
