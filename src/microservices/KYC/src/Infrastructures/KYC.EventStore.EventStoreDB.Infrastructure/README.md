# EventStore Infrastructure with EventStoreDB

## Prrequisites
- EventStoreDB
- Docker (For local environment)

## EventStoreDB Installation
Please check the following docker compose file from [docker-compose-eventstoredb.yml]([https://github.com/rabbicse/aspdotnetcore-ddd-cleanarchitecture-microservices/blob/master/src/microservices/KYC/docker-composes/docker-compose-mongodb.yml](https://github.com/rabbicse/aspdotnetcore-ddd-cleanarchitecture-microservices/blob/master/src/microservices/KYC/docker-composes/docker-compose-eventstoredb.yml)) 
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-eventstoredb.yml up -d
```

## References
- [EventSourcing.NetCore](https://github.com/oskardudycz/EventSourcing.NetCore/tree/main/Core.EventStoreDB)
