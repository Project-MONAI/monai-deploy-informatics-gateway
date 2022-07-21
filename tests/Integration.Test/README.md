<!--
  ~ Copyright 2022 MONAI Consortium
  ~
  ~ Licensed under the Apache License, Version 2.0 (the "License");
  ~ you may not use this file except in compliance with the License.
  ~ You may obtain a copy of the License at
  ~
  ~ http://www.apache.org/licenses/LICENSE-2.0
  ~
  ~ Unless required by applicable law or agreed to in writing, software
  ~ distributed under the License is distributed on an "AS IS" BASIS,
  ~ WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  ~ See the License for the specific language governing permissions and
  ~ limitations under the License.
-->

# MONAI Deploy Informatics Gateway Integration Test

The integration test suite is written using SpecFlow & Gherkin, a Behavior Driven Development (BDD) framework, and a domain-specific language designed for BDD.

## Development Requirements

- .NET 6
- [SpecFlow](https://specflow.org/)
- [Docker Compose](https://github.com/docker/compose/) 2.3+


## Running Integration Test

To run the integration test, first, update the `TAG`  value to one of the [available image versions](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/pkgs/container/monai-deploy-informatics-gateway) in the `.env.dev` file. Then, execute `./run.sh --dev` to start the test.

The script sets up the environment and starts docker-compose, pulling all required Docker images, including, RabbitMQ, MinIO, and Informatics Gateway.
