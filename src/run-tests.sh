# Copyright 2021 MONAI Consortium
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#     http://www.apache.org/licenses/LICENSE-2.0
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

#!/bin/bash

SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
TOP="$(git rev-parse --show-toplevel 2> /dev/null || readlink -f ${SCRIPT_DIR}/..)"
RESULTS_DIR=$SCRIPT_DIR/results
VERBOSITY=normal


if [ "$CI" = "true" ]; then
    VERBOSITY=minimal
fi

if [ -f /.dockerenv ]; then
    echo "##### Installing apt packages..."
    apt-get update
    apt-get install -y dcmtk sudo sqlite3
    git clean -fdx
fi

if [ ! -z ${CI} ]; then
    echo "##### Installing apt packages..."
    sudo apt-get update
    sudo apt-get install -y dcmtk sqlite3
    sudo git clean -fdx
fi


if [ -d "$RESULTS_DIR" ]; then 
    rm -r "$RESULTS_DIR"
fi

mkdir -p "$RESULTS_DIR"

echo "##### Building MONAI Deploy Informatics Gateway..."
cd $TOP/src
dotnet build Monai.Deploy.InformaticsGateway.sln

echo "Executing all tests"
dotnet test -v=$VERBOSITY --runtime linux-x64 --results-directory "$RESULTS_DIR" --collect:"XPlat Code Coverage" --settings "$SCRIPT_DIR/coverlet.runsettings" Monai.Deploy.InformaticsGateway.sln
exit $?