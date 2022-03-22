# SPDX-FileCopyrightText: © 2021-2022 MONAI Consortium
# SPDX-License-Identifier: Apache License 2.0

FROM mcr.microsoft.com/dotnet/sdk:6.0-focal as build

# Install the tools
RUN dotnet tool install --tool-path /tools dotnet-trace
RUN dotnet tool install --tool-path /tools dotnet-dump
RUN dotnet tool install --tool-path /tools dotnet-counters
RUN dotnet tool install --tool-path /tools dotnet-stack
WORKDIR /app
COPY . ./

RUN echo "Building MONAI Deploy Informatics Gateway..."
RUN dotnet publish -c Release -o out --nologo src/InformaticsGateway/Monai.Deploy.InformaticsGateway.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-focal

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get clean \
 && apt-get update \
 && apt-get install -y --no-install-recommends \
    libssl1.1 \
    openssl \
    sqlite3 \
   && rm -rf /var/lib/apt/lists

WORKDIR /opt/monai/ig
COPY --from=build /app/out .
#COPY docs/compliance/open-source-licenses.md .

COPY --from=build /tools /opt/dotnetcore-tools

EXPOSE 104
EXPOSE 5000

RUN ls -lR /opt/monai/ig
ENV PATH="/opt/dotnetcore-tools:${PATH}"

ENTRYPOINT ["/opt/monai/ig/Monai.Deploy.InformaticsGateway"]
