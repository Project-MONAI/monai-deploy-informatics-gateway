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

Before running the test suite, bring up all third-party dependencies using the docker compose file found in the `docker-compose` directory.

The test suite may be executed within Visual Studio's Test Explorer or using `dotnet test`.

```bash
dotnet test

dotnet test --filter AcrApi # run only the specified test feature
```

### Linux

On Linux, the `tests/Integration.Test/run.sh` script is available to bring up third-party dependencies & run the tests.


```bash
cd tests/Integration.Test
./run.sh

./run.sh -f AcrApi  # run only the specified test feature

```
