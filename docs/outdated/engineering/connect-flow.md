# Connect flow

How a client connects, drawn as stack traces. One block per thread, per tick, because
the work crosses both.

The database parts are **planned, not built**. Today `ConnectHandler` resolves the
account in memory and answers in the same tick. Names marked *(planned)* do not exist
yet. See [identity.md](identity.md) for the identifiers involved.

## Today

```
SERVER - one tick, game thread
  ServerApp.Run
    ReceiveAndHandlePackets
      HandlePacket
        ConnectHandler.Handle(request, endPoint)
          ILicenseValidator.IsValid(licenseKey)
          IAccountRepository.GetOrCreateAccountId(licenseKey)   -> in memory
          ClientRegistry.TryAddSession(accountId, playerId, endPoint)
          WorldSimulation.AddPlayer(playerId)
          IOutboundMessageSender.Send(ServerConnectionStatus)
```

## With the database

```
CLIENT - frame, before any of this
  Client.Update
    ServerConnection.Connect(licenseKey)
      MessageSerializer.Serialize(ClientConnectionRequest)
      Socket.SendTo
```

```
SERVER - tick N, game thread
  ServerApp.Run
    ReceiveAndHandlePackets
      Socket.ReceiveFrom
      HandlePacket
        MessageSerializer.Deserialize -> ClientConnectionRequest
        ConnectHandler.Handle(request, endPoint)
          ILicenseValidator.IsValid(licenseKey)            -> true
          PendingConnectionRegistry.TryAdd(...)            (planned)
          PersistenceWorker.Post(LoadAccount { RequestId, LicenseKey })   (planned)
                                                           -> returns immediately
    ClientSessionManager.DropIdleSessions
    WorldSimulation.Tick(delta)                            // this player is not in it
    BroadcastWorldView
    SleepForRemainderOfTick
```

```
DATABASE THREAD - between ticks, on its own          (planned)
  PersistenceWorker.Run
    BlockingCollection.Take()                              -> LoadAccount
    AccountStore.GetByLicenseKey(licenseKey)
      Dapper query                                         -> select ... from accounts where license_key = @key
    AccountStore.Insert(account)                           // only when the key is new
    Results.Add(AccountLoaded { RequestId, AccountId })
```

```
SERVER - tick N+k, game thread
  ServerApp.Run
    ReceiveAndHandlePackets
    ApplyPersistenceResults                                (planned)
      Results.TryTake()                                    -> AccountLoaded
      PendingConnectionRegistry.TryRemove(RequestId)
                                                           // no match: client gave up, drop it
      ClientRegistry.TryAddSession(accountId, playerId, endPoint)
        Base62SessionIdGenerator.Generate
        new ClientSession(sessionId, accountId, playerId, endPoint, now)
      WorldSimulation.AddPlayer(playerId)
      IOutboundMessageSender.Send(ServerConnectionStatus { Accepted, sessionId, playerId, zone name })
    PendingConnectionRegistry.DropExpired                  // rejection past the deadline
    ClientSessionManager.DropIdleSessions
    WorldSimulation.Tick(delta)                            // now includes this player
    BroadcastWorldView                                     // first view this client receives
```

```
CLIENT - next frame after the reply arrives
  Client.Update
    ServerConnection.PollIncoming
      MessageSerializer.Deserialize -> ServerConnectionStatus
      NetworkMessageDispatcher.Dispatch
        ServerAuthenticator.IsAuthenticated                -> from the server endpoint
        ConnectionStatusHandler.Handle(status)
          ServerConnection.ApplyConnectionStatus(status)   -> session id, player id, zone name
    ScreenManager.Update                                   -> shows the world
```

## Why it splits into two ticks

Nothing on the server waits for the database. `Post` returns at once, and the tick
continues for everyone else. The connect resumes on whichever later tick the result
arrives - usually the next one, since a local query takes a millisecond or two against
a 50 ms tick.

Failures use the same frames. `PersistenceFailed` replaces `AccountLoaded` and sends a
rejection instead, and `DropExpired` sends the same rejection when no result ever comes.

## Rules this shape depends on

- **A pending connection is not a session.** It holds only the license key, and it lives
  in its own collection, so anything iterating sessions never meets a half-connected one.
- **Results are matched by request id**, not by endpoint or session.
- **A repeat connect request is ignored** while one is pending for that endpoint and key.
  UDP means the client will resend, and each resend must not start another lookup.
- **Results drain before `Tick`**, so a new player exists for that tick's simulation and
  appears in that tick's broadcast.
