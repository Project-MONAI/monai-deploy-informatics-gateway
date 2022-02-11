# DEMO: MONAI Deploy App SDK App Runner Integration

This demo shows how an application integrates with the MONAI Deploy Informatics Gateway (MIG) by subscribing to events emitted by MIG and launches the configured MONAI Deploy Application Package (MAP).


![Demo-AppRunner](./demo-apprunner.png)

## Requirements

- MONAI Deploy Informatics Gateway 0.1.0+
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

## Other Ideas

💡 Instead of calling App Runner, integrate with [MIS](https://github.com/Project-MONAI/monai-deploy-app-server)
