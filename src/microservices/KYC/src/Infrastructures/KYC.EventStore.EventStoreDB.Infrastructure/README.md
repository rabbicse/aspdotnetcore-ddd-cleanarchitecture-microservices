# EventStore Infrastructure with EventStoreDB

## Prrequisites
- EventStoreDB
- Docker (For local environment)

## EventStoreDB Installation
The service is started by the shared compose file at [`docker-compose.yml`](../../../docker-compose.yml) in the KYC folder (see the [KYC README](../../../README.md)).
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-eventstoredb.yml up -d
```

## References
- [EventSourcing.NetCore](https://github.com/oskardudycz/EventSourcing.NetCore/tree/main/Core.EventStoreDB)
