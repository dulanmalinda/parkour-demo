# Colyseus Framework Overview

**Research Date:** 2025-11-14
**Framework Version:** Latest (as of 2025)
**Official Site:** https://colyseus.io/
**Documentation:** https://docs.colyseus.io/

## What is Colyseus?

Colyseus is a **free, open-source multiplayer framework for Node.js** that enables developers to build authoritative game servers. The platform emphasizes ownership—allowing creators to run their own servers rather than relying solely on third-party infrastructure.

## License

**MIT License** - permitting broad commercial and personal use

## Core Architecture

### Server Technology Stack
- Built on **Node.js**
- Uses **WebSockets** for real-time bidirectional communication
- Written in **JavaScript/TypeScript**

### Key Design Principles
- **Authoritative server architecture** - designed to prevent client-side exploits
- **Room-based system** - on-demand, stateful rooms are spawned per client request
- **Binary state synchronization** - uses custom delta serializer for efficiency
- **MessagePack** for custom messages - fastest serializer available for JavaScript

## Primary Features

### 1. Automatic State Synchronization
- Server state is automatically synchronized to all connected clients
- Binary patches of state are sent to clients at configurable intervals
- Default patch rate: **50ms (20fps)** - adjustable per room
- Only changed properties are transmitted (delta encoding)

### 2. Room-Based Matchmaking
- From a single Room definition, clients are matched into multiple Room instances
- Automatically connects players into game sessions
- On-demand room creation and destruction

### 3. Horizontal Scalability
- Built for scaling across multiple processes/machines
- Requires presence server for distributed deployments
- Can handle millions of players across different game rooms

### 4. Multi-Platform SDK Support
Official SDKs available for:
- JavaScript/TypeScript
- **Unity** ⭐
- Construct 3
- Defold Engine
- Haxe 4
- Cocos Creator

## Community & Adoption

### Statistics
- **759K+ npm downloads**
- **6.4K GitHub stars**
- **80+ contributors**

### Notable Games Using Colyseus
- Pixels.xyz
- Bloxd.io
- Unboxing the Cryptic Killer
- Many indie and studio-developed titles

## Deployment Options

### Self-Hosted
- Full control over server infrastructure
- Run on your own hardware/cloud provider
- Complete code ownership

### Colyseus Cloud (Managed Hosting)
- Starting at **$15/month**
- Eliminates operational overhead
- Turnkey scalable solution

## Why Colyseus for Parkour Multiplayer Prototype?

### Advantages
✅ **Fast prototyping** - automatic state sync reduces boilerplate
✅ **Unity SDK** - official first-class support
✅ **Authoritative server** - prevents cheating in competitive parkour
✅ **Room-based** - easy to create parkour challenge rooms
✅ **Active development** - documentation updated November 2025
✅ **Free & open-source** - perfect for prototyping
✅ **TypeScript/JavaScript** - familiar web technology stack

### Considerations
⚠️ Requires Node.js server setup and management
⚠️ Learning curve for state schema definitions
⚠️ Need to handle server-side physics for authoritative movement

## Related Documentation
- [Unity Integration](./unity-integration.md)
- [Server Architecture](./server-architecture.md)
- [State Synchronization](./state-synchronization.md)

## References
- Official Website: https://colyseus.io/
- Documentation: https://docs.colyseus.io/
- GitHub: https://github.com/colyseus/colyseus
- Unity SDK: https://github.com/colyseus/colyseus-unity-sdk
