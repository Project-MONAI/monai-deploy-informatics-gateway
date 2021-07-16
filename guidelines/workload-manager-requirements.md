# MONAI Workload Manager Requirements

## Overview

The MONAI Workload Manager (MWM) routes data received by MONAI Informatics Gateway to the correct applications and routes any data produced by the applications back to configured hospital information systems (HIS).  

This document defines the requirements for the MONAI Workload Manager.

## Goal
The goal for this proposal is to enlist, prioritize and provide clarity on the requirements for MONAI Workload Manager. Developers working on different software modules in MONAI Workload Manager SHALL use this specification as a guideline when designing and implementing software for the MONAI Workload Manager.

## Standard Language
This document SHALL follow the guidance of [rfc
2119](https://datatracker.ietf.org/doc/html/rfc2119) for terminology.

## Success Criteria
Data SHALL be routed to the user-defined applications and results, if any, SHALL be routed back to configured HIS.


## Requirements

### Data Ingestion

#### MWM SHALL be able to receive notifications upon data enters MONAI Deploy.

#### MWM SHALL be able to receive notifications upon data enters MONAI Deploy without triggering app discovery engine.

#### MWM SHALL be able to discover applications deployed on MONAI Deploy.

#### MWM SHALL respect user-defined data discovery rules.

#### MWM SHALL be able to route incoming data to one or more deployed applications.


### Data Export

#### MWM SHALL support multiple export destinations (sink).

#### MWM SHALL be able to route data to multiple sinks.

#### MWM SHALL allow users to create custom sinks.

### Functional Requirements

#### MWM SHALL be able to support multiple orchestration engines.

#### MWM SHALL track status of all jobs initiated with orchestration engines.

#### MWM SHALL remove payloads only upon successful export.
