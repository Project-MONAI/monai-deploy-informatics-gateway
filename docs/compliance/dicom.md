# DICOM Interface

The following reference describes the connectivity capabilities of MONAI Deploy SDK out of the box.
Users implementing the MONAI Deploy SDK must update their DICOM Conformance Statement according
to the actual capabilities of their application.

## DICOM SCP

The DICOM SCP service implements C-ECHO and C-Store services to interface with other medical devices,
such as PACS. It allows users to define multiple AE Titles to enable DICOM communication. It then
maps each AE Title to a pipeline.

### DIMSE Services (SCP)

- **C-STORE**: Accepts incoming DICOM objects
- **C-ECHO**: Accepts incoming DICOM verification requests

### SOP Classes (Transfer) and Transfer Syntax Supported

The DICOM SCP service accepts any proposed transfer syntaxes and stores any accepted instances, as-is, on
disk without any decoding support.

### Association Policies

- DICOM Storage SCP accepts associations but does not initiate associations.
- DICOM Storage SCP accepts up to `1000` (default: `25`) simultaneous associations across all configured AE Titles.
- DICOM Storage SCP accepts associations when storage space usage is less than the configured watermark (default: `75%` of the storage partition) and the available storage space is above the configured reserved storage size (default: 5GB of free space).
- Asynchronous mode is not supported. Instead, all operations are performed synchronously.
- The Implementation Class UID is `1.3.6.1.4.1.30071.8` and the Implementation Version Name is
  `fo-dicom 4.x.x`.
- An association must be released properly for received instances to be associated with a pipeline.
  Files received from an aborted association or an interrupted connection are either removed
  immediately or removed based on a configured timeout value.

### Security Profiles

MONAI Deploy Informatics Gateway Storage SCP does not conform to any defined DICOM Security Profiles. Therefore, the product is assumed to be used within a secure environment with a firewall, router protection, VPN, and other network security provisions.

Users may configure the DICOM Storage SCP service to check the following DICOM values when
determining whether to accept Association Open Requests:

- Calling (source) AE Title - to accept DICOM instances from known sources.
- Called (MONAI SCP) AE Title - to accept DICOM instance through configured MONAI AETs.

## DICOM SCU

The DICOM Storage SCU provides the DICOM Storage Service for interfacing with other medical
devices such as PACS. It is executed at system startup and exists in a container using a single
configurable AE Title.

### DIMSE Services (SCU)

**C-STORE**: Sends user-generated DICOM results to configured destinations.

The DICOM Storage SCU initiates a push of DICOM objects to the Remote DICOM Storage SCP.
The system allows multiple remote SCPs to be configured.

### SOP Classes (Transfer) Supported and Transfer Syntax

- The DICOM Store SCU service supports all SOP Classes of the Storage Service Class.
- The DICOM Store SCU service transfers a DICOM object as-is using the stored Transfer Syntax,
without the support of compression, decompression, or Transfer Syntax conversion.

### Association Policies

- DICOM Storage SCU initiates associations but does not accept associations.
- DICOM Storage SCU allows two (configurable) SCU instances simultaneously.
- Asynchronous mode is not supported. All operations are performed synchronously.
- The Implementation Class UID is `1.3.6.1.4.1.30071.8` and the Implementation Version Name is
  `fo-dicom 4.x.x`.

### Security Profiles

Not applicable
