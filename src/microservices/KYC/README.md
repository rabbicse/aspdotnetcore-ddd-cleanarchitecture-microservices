# KYC Service
KYC service is written in ASP.NET Core and C# with the following patterns and principles.
- DDD
- Clean Architecture
- Event Driven Architecture
- CQRS
- Distributed Caching
- Asynchronous REST API
- Background processing

## Apache Kafka Cluster on docker
Please check the following docker compose file from [docker-compose-kafka.yml](https://github.com/rabbicse/aspdotnetcore-ddd-cleanarchitecture-microservices/blob/master/src/microservices/KYC/docker-composes/docker-compose-kafka.yml) 
and write the following command to run on local machine. Make sure you've installed Docker Engine for Linux or Docker Desktop for Windows/MacOS.

```
docker compose -f docker-compose-kafka.yml up -d
```

It will install on docker engine:
- 1 zookeeper server on port 2181
- 3 kafka brokers on ports 9092, 9093, 9094
- 1 kafdrop ui console on port 9000

To show kafdrop ui visit: http://localhost:9000

To access kafka brokers outside docker hosted ip, add the environment varible: DOCKER_HOST_IP. For linux write the following command.

```
export DOCKER_HOST_IP=192.168.0.100
```

Here, 192.168.0.100 is docker hosted ip address.
