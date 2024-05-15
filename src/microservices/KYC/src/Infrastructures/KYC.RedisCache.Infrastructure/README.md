# Distributed Cache 
## Setup Redis Stack on docker
Please check the following docker compose file from [docker-compose-redis.yml](https://github.com/rabbicse/aspdotnetcore-ddd-cleanarchitecture-microservices/blob/master/src/microservices/KYC/docker-composes/docker-compose-redis.yml) 
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-redis.yml up -d
```

It will install on docker engine:
- 1 redis server on port 6379
- 1 redis insight ui console on port 8001

To show redis insight ui console visit: http://localhost:8001
