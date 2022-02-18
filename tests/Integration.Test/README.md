# MONAI Deploy Informatics Gateway Integration Test

The integration test suite is written using SpecFlow & Gherkin, a Behavior Driven Development (BDD) framework, and a domain-specific language designed for BDD.

## Development Requirements

- .NET 6
- [SpecFlow](https://specflow.org/)
- [Docker Compose](https://docs.docker.com/compose/)


## Running Integration Test

To run the integration test, first update the `TAG`  value to one of the [available image versions](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/pkgs/container/monai-deploy-informatics-gateway) in the `.env.dev` file. Then, execute `./run.sh` to start the test.

The script sets up the environment and starts docker-compose, pulling all required Docker images, including, RabbitMQ, MinIO, and Informatics Gateway.
