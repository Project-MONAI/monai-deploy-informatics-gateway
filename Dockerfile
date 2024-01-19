# Copyright 2021-2022 MONAI Consortium
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy as build

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
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy

# Enable elastic client compatibility mode
ENV ELASTIC_CLIENT_APIVERSIONING=true
ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get clean \
 && apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && apt-get install -y libc6-dev=2.35-0ubuntu3.6                          # this is a workaround for Mongo encryption library
RUN rm -rf /var/lib/apt/lists



WORKDIR /opt/monai/ig

COPY --from=build /app/out .
COPY --from=build /tools /opt/dotnetcore-tools
COPY LICENSE ./
COPY docs/compliance/third-party-licenses.md ./

RUN ln -s /usr/lib/x86_64-linux-gnu/libdl.so.2 /opt/monai/ig/libdl.so    # part 2 of workaround for Mongo encryption library

EXPOSE 104
EXPOSE 2575
EXPOSE 5000

HEALTHCHECK --interval=10s --retries=10 CMD curl --fail http://localhost:5000/health || exit 1

RUN ls -lR /opt/monai/ig
ENV PATH="/opt/dotnetcore-tools:${PATH}"

ENTRYPOINT ["/opt/monai/ig/Monai.Deploy.InformaticsGateway"]
