# Setup


## Installation

tbd

## Configure Storage Mount

tbd

> [!Note]
> To change the default storage location on the host machine , find and modify the `hostPath` property in
> `~/.clara/charts/dicom-adapter/values.yaml` and restart the DICOM Adapter.

> [!Note]
> The default size of the persistent volume claim created for the mount is 50Gi.
> To increase or decrease the size of the volume claim, find and modify the `volumeSize` property in
> `~/.clara/charts/dicom-adapter/values.yaml` and restart the DICOM Adapter.

## Configure Informatics Gateway

tbd


Please refer to [Configuration Schema](schema.md) for a complete reference.


> [!Note]
> Before running Informatics Gateway, adjust the values of `watermarkPercent` and `reserveSpaceGB` based on
> the expected number of studies and size of each study. Suggested value for `reserveSpaceGB` is 2x to 3x the
> size of a single study multiply by the number of configured Clara AE Titles.

> [!Note]
> If Informatics Gateway is restarted before a C-STORE-RQ completes, the association is properly released or 
> before it was able to create a job, then the received DICOM instances were dropped upon restart.



## Start Informatics Gateway

tbd

## Enable Incoming Associations

tbd

## Export Processed Results

tbd

## Summary

tbd
