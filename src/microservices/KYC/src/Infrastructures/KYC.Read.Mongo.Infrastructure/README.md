# Read Infrastructure with MongoDB

## Prrequisites
- MongoDB
- Docker (For local environment)

## MongoDB Installation
Please check the following docker compose file from [docker-compose-mongodb.yml](https://github.com/rabbicse/aspdotnetcore-ddd-cleanarchitecture-microservices/blob/master/src/microservices/KYC/docker-composes/docker-compose-mongodb.yml) 
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-mongodb.yml up -d
```

## References
- [ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing](https://github.com/jeangatto/ASP.NET-Core-Clean-Architecture-CQRS-Event-Sourcing/tree/main/src/Shop.Query)
