namespace ParkourLegion.Schema
{
    public class PlayerState : Colyseus.Schema.Schema
    {
        [Colyseus.Schema.Type(0, "string")]
        public string id = "";

        [Colyseus.Schema.Type(1, "string")]
        public string name = "Player";

        [Colyseus.Schema.Type(2, "float32")]
        public float x = 0;

        [Colyseus.Schema.Type(3, "float32")]
        public float y = 1;

        [Colyseus.Schema.Type(4, "float32")]
        public float z = 0;

        [Colyseus.Schema.Type(5, "float32")]
        public float rotY = 0;

        [Colyseus.Schema.Type(6, "uint8")]
        public byte movementState = 0;

        [Colyseus.Schema.Type(7, "boolean")]
        public bool isGrounded = true;

        [Colyseus.Schema.Type(8, "uint8")]
        public byte lastCheckpoint = 0;

        [Colyseus.Schema.Type(9, "uint8")]
        public byte skinId = 0;
    }
}
