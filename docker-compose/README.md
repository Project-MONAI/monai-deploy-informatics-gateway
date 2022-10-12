# MONAI Deploy Informatics Gateway

The [docker-compose.yml](./docker-compose.yml) file includes the following services to run the Informatics Gateway.

* MinIO
* RabbitMQ
* ElasticSearch (optional)
* LogStash (optional)
* Kibana (optional)


## Running docker compose

To start all Informatics Gateway dependencies, run `docker compose up`.

Before running Informatics Gateway, ensure the following environment variables are exported:

```bash
export DOTNET_ENVIRONMENT=Development # if using appsettings.Development.json
export LOGSTASH_URL=tcp://localhost:50000 # this tells Informatics Gateway to export logs to LogStash at tcp://localhost:5000
```
IMPORTANT: for Linux users, before running `docker compose up`, please run `init.sh` to create directories with the correct permissions first. Otherwise, ElasticSearch will not be able to start.

### Kibana

A default search is imported to Kibana at startup. To load the saved search, go to Analytics > Discover from the 🍔 menu. From the top right click *Open* and select *MONAI-Default*.
