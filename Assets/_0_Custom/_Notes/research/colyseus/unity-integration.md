# Colyseus Unity SDK Integration

**Research Date:** 2025-11-14
**SDK Repository:** https://github.com/colyseus/colyseus-unity-sdk
**Documentation:** https://docs.colyseus.io/getting-started/unity

## Installation Methods

### Method 1: UPM (Unity Package Manager) - RECOMMENDED
1. Open Unity Editor
2. Go to **Window > Package Manager**
3. Click the **"+"** button
4. Select **"Add package from git URL"**
5. Enter: `https://github.com/colyseus/colyseus-unity3d.git#upm`

### Method 2: Legacy Unity Package
1. Download latest `.unitypackage` from [releases page](https://github.com/colyseus/colyseus-unity3d/releases/latest)
2. Import into Unity project via **Assets > Import Package > Custom Package**

## Initial Setup

### Basic Client Initialization

```csharp
using Colyseus;

// Initialize client with server WebSocket address
ColyseusClient client = new ColyseusClient("ws://localhost:2567");
```

**For Local Development:**
- Default server address: `ws://localhost:2567`

**For Production:**
- Use your deployed server URL: `ws://your-server.com`
- Or Colyseus Cloud URL

## Room Connection Methods

Colyseus provides **4 primary approaches** to connect to rooms:

### 1. Create
Establishes a new room instance
```csharp
ColyseusRoom<MyRoomState> room = await client.Create<MyRoomState>("room_name");
```

### 2. Join
Enters an existing room with available slots
```csharp
ColyseusRoom<MyRoomState> room = await client.Join<MyRoomState>("room_name");
```

### 3. JoinById
Connects to a specific room by ID
```csharp
ColyseusRoom<MyRoomState> room = await client.JoinById<MyRoomState>("room_id");
```

### 4. JoinOrCreate - RECOMMENDED FOR MATCHMAKING
Automatically matchmakes or creates a room if none exist
```csharp
ColyseusRoom<MyRoomState> room = await client.JoinOrCreate<MyRoomState>("room_name");
```

**Pass Custom Options:**
```csharp
var options = new Dictionary<string, object>() {
    { "maxPlayers", 8 },
    { "mapName", "parkour_01" }
};
ColyseusRoom<MyRoomState> room = await client.JoinOrCreate<MyRoomState>("parkour_room", options);
```

## Room Events

The `ColyseusRoom` class provides key lifecycle events:

### OnJoin
Triggers after successful connection
```csharp
room.OnJoin += () => {
    Debug.Log("Successfully joined room!");
};
```

### OnLeave
Fires upon disconnection
```csharp
room.OnLeave += (code) => {
    Debug.Log($"Left room with code: {code}");
};
```

### OnStateChange
Activates when room state updates occur
```csharp
room.OnStateChange += (state, isFirstState) => {
    if (isFirstState) {
        Debug.Log("Received initial state");
    }
};
```

### OnError
Reports server-side room errors
```csharp
room.OnError += (code, message) => {
    Debug.LogError($"Room error {code}: {message}");
};
```

## Communication Patterns

### Listen for Custom Messages from Server
```csharp
room.OnMessage<MessageType>("eventName", (message) => {
    // Handle message
    Debug.Log($"Received message: {message}");
});
```

**Example for Player Actions:**
```csharp
room.OnMessage<PlayerJumpData>("player_jump", (data) => {
    Debug.Log($"Player {data.playerId} jumped at {data.position}");
});
```

### Send Custom Messages to Server
```csharp
room.Send("action_type", new {
    x = 1.3f,
    y = -1.4f,
    action = "jump"
});
```

**Example for Parkour Actions:**
```csharp
// Send wall-run action
room.Send("wallrun", new {
    playerId = myPlayerId,
    wallNormal = new { x = 1, y = 0, z = 0 },
    position = transform.position
});
```

## Testing Multiple Clients in Editor

### Unity 6000.1.0b1+ (Built-in Multiplayer Play Mode)
Unity includes **Multiplayer Play Mode** for testing multiple clients simultaneously without building.

**How to Use:**
1. Enable Multiplayer Play Mode in Editor settings
2. Configure number of virtual players
3. Test directly in editor

### Older Unity Versions - ParrelSync
For Unity versions before 6000.1.0b1, use **ParrelSync** package:
- Creates editor clones for simultaneous testing
- Available on Unity Asset Store or GitHub

## State Schema Integration

**Define your room state class:**
```csharp
using Colyseus.Schema;

public class MyRoomState : Schema {
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();

    [Type(1, "number")]
    public float gameTime = 0;
}

public class Player : Schema {
    [Type(0, "string")]
    public string id;

    [Type(1, "number")]
    public float x;

    [Type(2, "number")]
    public float y;

    [Type(3, "number")]
    public float z;
}
```

**Access state in Unity:**
```csharp
room.State.players.OnAdd += (key, player) => {
    Debug.Log($"Player {key} joined!");
    SpawnPlayer(player);
};

room.State.players.OnRemove += (key, player) => {
    Debug.Log($"Player {key} left!");
    DestroyPlayer(player);
};
```

## Best Practices for Unity Integration

### 1. Use Async/Await Pattern
```csharp
async void Start() {
    try {
        ColyseusRoom<MyRoomState> room = await client.JoinOrCreate<MyRoomState>("parkour");
        SetupRoomHandlers(room);
    } catch (Exception e) {
        Debug.LogError($"Failed to connect: {e.Message}");
    }
}
```

### 2. Clean Up on Disconnect
```csharp
void OnApplicationQuit() {
    room?.Leave();
}

void OnDestroy() {
    room?.Leave();
}
```

### 3. Handle Connection Failures
```csharp
private async void TryConnect() {
    int retries = 3;
    while (retries > 0) {
        try {
            room = await client.JoinOrCreate<MyRoomState>("parkour");
            break;
        } catch (Exception e) {
            retries--;
            Debug.LogWarning($"Connection attempt failed, retries left: {retries}");
            await Task.Delay(1000);
        }
    }
}
```

## Next Steps for Parkour Prototype

1. Install Unity SDK via UPM
2. Set up Node.js server (see [Server Architecture](./server-architecture.md))
3. Define state schema for parkour game (see [State Synchronization](./state-synchronization.md))
4. Implement player movement synchronization
5. Test with Multiplayer Play Mode or ParrelSync

## Related Documentation
- [Overview](./overview.md)
- [Server Architecture](./server-architecture.md)
- [State Synchronization](./state-synchronization.md)

## References
- Unity SDK Documentation: https://docs.colyseus.io/getting-started/unity
- GitHub Repository: https://github.com/colyseus/colyseus-unity-sdk
- Example Projects: https://github.com/colyseus/colyseus-unity-sdk/tree/master
