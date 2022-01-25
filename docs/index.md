<p align="center">
  <img src="https://raw.githubusercontent.com/Project-MONAI/MONAI/dev/docs/images/MONAI-logo-color.png" width="50%" alt='project-monai'>
</p>

💡 If you want to know more about MONAI Deploy WG vision, overall structure, and guidelines, please read <https://github.com/Project-MONAI/monai-deploy> first.

# MONAI Deploy Informatics Gateway

MONAI Deploy Informatics Gateway (MIG), handles the first and last step in a clinical data pipeline, the Input and Output (I/O). 

It uses standard protocols like DICOM and FHIR, stores studies and resources from the medical record system as payloads, and notifies the rest of the MONAI Deploy system, specifically the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager), that data is ready to be processed. 

After inference is done, it receives the results and send them to the proper consumers, usually PACS or viewers for visualization, VNAs for storage, and EHRs (Electronic Healthcare Records).  

## Services

*MONAI Deploy Informatics Gateway* contains the following standard protocols to communicate with your medical devices:

* **DICOM SCP**: C-ECHO, C-STORE
* **DICOM SCU**: C-STORE
* **ACR DSI API**: [The American College of Radiology’s Data Science Institute API](https://www.acrdsi.org/-/media/DSI/Files/ACR-DSI-Model-API.pdf)
  * **DICOMweb client**: QIDO-RS, WADO-RS, STOW-RS
  * **FHIR client**: GET

### DICOM SCP

The *DICOM SCP Service* accepts standard DICOM C-ECHO and C-STORE commands, which receive DICOM instances for processing. The received instances are stored immediately to the configured temporary storage location (`InformaticsGateway>storage>temporary`) and then uploaded to the [MONAI Deploy Workflow Manager](https://github.com/Project-MONAI/monai-deploy-workflow-manager). All DICOM instances are stored on disk as-is using the original transfer syntax described in
the [DICOM Interface](./compliance/dicom.md#dicom-scp) section. The MONAI Deploy application developer must handle any encoding/decoding of the DICOM files within the applications. Please refer to the [MONAI Deploy App SDK](https://github.com/Project-MONAI/monai-deploy-app-sdk) for further information.

### DICOM SCU

The *DICOM SCU Service* enables users to export application-generated DICOM results to external DICOM devices. It queries the `Results.Get` API from the [MONAI Deploy Workload Manager](https://github.com/Project-MONAI/monai-deploy-workload-manager) to retrieve user-generated DICOM results assigned to the `MONAISCU` sink (`InformaticsGateway/dicom/scu/sink`).

> [!Note]
> DICOM instances are sent as-is; no codec conversion is done as part of the SCU process. 
> See the [DICOM Interface SCU](./compliance/dicom.md#dimse-services-scu) section for more information.

### ACR DSI API

The ACR DSI API enables users to trigger inference requests via RESTful calls, utilizing DICOMweb & FHIR to retrieve data specified in the request.  Upon data retrieval, the Informatics Gateway forwards the data to the [MONAI Deploy Workload Manager](https://github.com/Project-MONAI/monai-deploy-workload-manager) for job scheduling.

## Installation
TO BE ADDED

## Getting started
TO BE ADDED

### Development Requirements

* .NET 5.0

### Runtime Requirements

* Docker 20.10.7 or later

## Contributing
For guidance on making a contribution, see the [contributing guidelines](https://github.com/Project-MONAI/monai-deploy/blob/main/CONTRIBUTING.md).

## Community
To participate in the MONAI Deploy WG, please review <https://github.com/Project-MONAI/MONAI/wiki/Deploy-Working-Group>.

Join the conversation on Twitter [@ProjectMONAI](https://twitter.com/ProjectMONAI) or join our [Slack channel](https://forms.gle/QTxJq3hFictp31UM9).

Ask and answer questions over on [MONAI Deploy Informatics Gateway's GitHub Discussions tab](https://github.com/Project-MONAI/monai-deploy-informatics-gateway/discussions).

## Links

- Website: <https://monai.io>
- API documentation: <https://docs.monai.io/projects/monai-deploy-informatics-gateway>
- Code: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway>
- Project tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/projects>
- Issue tracker: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/issues>
- Wiki: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/wiki>
- Test status: <https://github.com/Project-MONAI/monai-deploy-informatics-gateway/actions>


