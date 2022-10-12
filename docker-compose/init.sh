#!/bin/bash
# Copyright 2022 MONAI Consortium
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

RUNDIR=$PWD/.run

echo "Initializing directories..."
[ -d $RUNDIR ] && echo "Removing existin $RUNDIR" && sudo rm -r $RUNDIR
mkdir -p $RUNDIR/esdata/ && echo "Created $RUNDIR/"
sudo chown 1000:1000 -R $RUNDIR/esdata && echo "Permission updated"
echo "Directories setup"
echo "Ready to run docker compose up"
