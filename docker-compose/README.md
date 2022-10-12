<!-- 
Copyright 2022 MONAI Consortium

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. 
-->

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
