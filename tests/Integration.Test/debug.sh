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


# enable(1)/disable(0) VS code attach debuger
export VSTEST_HOST_DEBUG=0

export SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null && pwd)"
TEST_DIR="$SCRIPT_DIR/"
LOG_DIR="${GITHUB_WORKSPACE:-$SCRIPT_DIR}"
RUN_DIR="$SCRIPT_DIR/.run"
BIN_DIR="$TEST_DIR/bin/Release/net6.0"
CONFIG_DIR="$SCRIPT_DIR/configs"
EXIT=false
METRICSFILE="$LOG_DIR/metrics.log"
LOADDEV="--env-file .env.dev"
STREAMID=
export STUDYJSON="study.json.dev"

set -euo pipefail

function info() {
    echo "$(date -u '+%Y-%m-%d %H:%M:%S') [INFO]:" $@
}

function fatal() {
    echo >&2 "$(date -u '+%Y-%m-%d %H:%M:%S') [FATAL]:" $@
    tear_down
    exit 1
}

function check_status_code() {
    STATUS=$1
    info "STATUS=$STATUS"
    if [[ $STATUS == 400 ]] || [[ $STATUS == 500 ]] || [[ $STATUS == 000 ]]; then
        fatal 'HTTP call failed with exit code $STATUS'
    fi
}

function env_setup() {
    [ -d $RUN_DIR ] && info "Removing $RUN_DIR..." && sudo rm -r $RUN_DIR
    mkdir -p $RUN_DIR

    [ -d $BIN_DIR ] && info "Removing $BIN_DIR..." && sudo rm -r $BIN_DIR

    if [[ $(docker-compose ps -q | wc -l) -ne 0 ]]; then
        info "Stopping existing services..."
        docker-compose $LOADDEV down
    fi

    if (dotnet tool list --global | grep livingdoc &>/dev/null); then
        info "Upgrading SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool update --global SpecFlow.Plus.LivingDoc.CLI
    else
        info "Installing SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
    fi

    info "LOG_DIR = $LOG_DIR"
}

function build() {
    pushd $SCRIPT_DIR
    info "Building test runner..."
    dotnet build -c Release
}

function start_services() {
    info "Starting dependencies docker-compose $LOADDEV up -d --force-recreate..."
    docker-compose $LOADDEV up -d --force-recreate

    HOST_IP=$(docker network inspect testrunner | jq -r .[0].IPAM.Config[0].Gateway)
    info "Host IP = $HOST_IP"
    export HOST_IP

    info "============================================="
    docker container ls --format 'table {{.Names}}\t{{.ID}}' | grep integrationtest
    info "============================================="
    docker network inspect testrunner
    info "============================================="

    info "Stopping Informatics Gateway for debugging..."
    ig_contianer=$(docker container ls --format 'table {{.Names}}\t{{.ID}}' | grep integrationtest-informatics-gateway | awk '{print $2}')
    docker kill $ig_contianer

    sleep 1
    sudo chown -R $USER:$USER $RUN_DIR
}


function tear_down() {
    set +e
    info "Stop streaming metrics log..."
    kill $STREAMID >/dev/null 2>&1
    set -e

    info "Stopping services..."
    docker-compose $LOADDEV down --remove-orphans
}

function main() {
    env_setup "$@"
    build
    start_services
    docker-compose logs -f
}

main "$@"
