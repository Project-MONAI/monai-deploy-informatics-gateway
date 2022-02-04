# MONAI Deploy Informatics Gateway <small>v0.0.0</small>

![NVIDIA](./images/MONAI-logo_color.svg)

## Introduction

MONAI Deploy Informatics Gateway (MIG) handles the first and last step in a clinical data pipeline, the Input, and Output (I/O).

MIG uses standard protocols like DICOM and FHIR. It stores studies and resources from the medical record system as payloads. It then notifies the rest of the MONAI Deploy system, specifically the MONAI Deploy Workflow Manager, that data is ready to be processed.

After inference completes, MIG receives notifications for exporting the results to the proper consumers, usually PACS or viewers for visualization, VNAs for storage, and EHRs (Electronic Healthcare Records).


## Services

*MONAI Deploy Informatics Gateway* contains the following standard protocols to communicate with your medical devices:

* **DICOM SCP**: C-ECHO, C-STORE
* **DICOM SCU**: C-STORE
* **ACR DSI API**: [The American College of Radiology’s Data Science Institute API](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf)
  * **DICOMweb client**: QIDO-RS, WADO-RS, STOW-RS
  * **FHIR client**: GET

### DICOM SCP

The *DICOM SCP Service* accepts standard DICOM C-ECHO and C-STORE commands, which receive DICOM instances for processing. The received instances are stored immediately to the configured temporary storage location (`InformaticsGateway>storage>temporary`) and then uploaded to the shared storage for the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager) to consume. All DICOM instances are stored on disk as-is using the original transfer syntax described in
the [DICOM Interface](./compliance/dicom.md#dicom-scp) section. The MONAI Deploy application developer must handle any encoding/decoding of the DICOM files within the applications. Please refer to the [MONAI Deploy App SDK](https://github.com/Project-MONAI/monai-deploy-app-sdk) for further information.

### DICOM SCU

The *DICOM SCU Service* enables users to export application-generated DICOM results to external DICOM devices. It subscribes to the `md.export.request.monaiscu` events emitted by the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager) and then export the data to user-configured DICOM destination(s).

> [!Note]
> DICOM instances are sent as-is; no codec conversion is done as part of the SCU process. 
> See the [DICOM Interface SCU](./compliance/dicom.md#dimse-services-scu) section for more information.

### ACR DSI API

The ACR DSI API enables users to trigger inference requests via RESTful calls, utilizing DICOMweb & FHIR to retrieve data specified in the request. Upon data retrieval, the Informatics Gateway uploads the data to the shared storage and emits an `md.workflow.request` event, which notifies the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager) to process.

#### DICOMweb Export

A DICOMweb export agent can export any user-generated DICOM contents to configured DICOM destinations. The agent subscribes to the `md.export.request.monaidicomweb` events emitted by the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager) and then export the data to user-configured DICOMweb destination(s).
