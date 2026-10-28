# Read Infrastructure with MongoDB

## Prrequisites
- MongoDB
- Docker (For local environment)

## MongoDB Installation
The service is started by the shared compose file at [`docker-compose.yml`](../../../docker-compose.yml) in the KYC folder (see the [KYC README](../../../README.md)).
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-mongodb.yml up -d
```

## References
- [ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing](https://github.com/jeangatto/ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing/tree/main/src/Shop.Query)
