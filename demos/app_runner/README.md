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

# DEMO: MONAI Deploy App SDK App Runner Integration

This demo shows how an application integrates with the MONAI Deploy Informatics Gateway (MIG) by subscribing to events emitted by MIG and launches the configured MONAI Deploy Application Package (MAP).


![Demo-AppRunner](./demo-apprunner.png)

## Requirements

- MONAI Deploy Informatics Gateway 0.1.1+
- MONAI Deploy App SDK 0.2.1+
  - A MAP from the [examples](https://github.com/Project-MONAI/monai-deploy-app-sdk/tree/main/examples/apps/) or BYO MAP.
- RabbitMQ configured and running
- MinIO configured and running
- Python 3.7+


## Running the demo

1. Install requirements specified above
2. Configure an AET with one or more workflows. For example, the following command would trigger the `dcm-to-img:latest` MAP.
    ```
    mig-cli  aet add -a DCM2PNG -w dcm-to-img:latest -v
    ```
3. Install python dependencies specified in [requirements.txt](./requirements.txt)
4. Edit `config.json` and change:
   1. `endpoint`/`host`, `username`, and `password` for both storage and messaging services
   2. `bucket` where payloads are stored
5. python app.py

**Notes**: For MONAI Deploy App SDK 0.2.1, set `ignore_json` to `false` in the `config.json` file so DICOM JSON files are not downloaded.

## Job Directory Structure

Both retrieved input dataset and MAP generated output are stored under the `jobs/` directory which is configurable in the
`config.json` file under `working_dir`.

A subdirectory is created for each request received using the `correlation_id` specified in the payload.
Given that one or more workflows can be specified in the payload, output generated from each MAP is stored under `jobs/{correlation_id}/output/{map_name}`.

Sample output of `jobs` directory:
```
└── db61c4fc-b84e-4255-b40a-60254f93ca6f (correlation_id of the request)
    ├── input
    └── output
        └── dcm-to-img-latest (name of the workflow/MAP)
```

## Other Ideas

💡 Instead of calling App Runner, integrate with [MIS](https://github.com/Project-MONAI/monai-deploy-app-server)
