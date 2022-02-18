#!/bin/bash

export SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" >/dev/null && pwd)"
TEST_DIR="$SCRIPT_DIR/"
RUN_DIR="$SCRIPT_DIR/.run"
BIN_DIR="$TEST_DIR/bin/Release/net6.0"
CONFIG_DIR="$SCRIPT_DIR/configs"
EXIT=false
METRICSFILE=$SCRIPT_DIR/metrics.log
LOADDEV=

export DATA_PATH="$RUN_DIR/ig/payloads/"
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
    if [[ $(docker-compose ps -q | wc -l) -ne 0 ]]; then
        info "Stopping existing services..."
        docker-compose  $LOADDEV down
    fi

    if (dotnet tool list --global | grep livingdoc &>/dev/null)  ; then
        info "Upgrading SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool update --global SpecFlow.Plus.LivingDoc.CLI
    else
        info "Installing SpecFlow.Plus.LivingDoc.CLI..."
        dotnet tool install --global SpecFlow.Plus.LivingDoc.CLI
    fi

    info "Removing existing test data..."
    [ -d $RUN_DIR ] && sudo rm -r $RUN_DIR
    mkdir -p $RUN_DIR

    info "Removing bin..."
    [ -d $BIN_DIR ] && sudo rm -r $BIN_DIR

    set +u
    if [ "$1" = "--dev" ] ; then
        info "Using .env.dev..."
        LOADDEV="--env-file .env.dev"
    fi
    set -u
}

function build() {
    pushd $SCRIPT_DIR
    info "Building test runner..."
    dotnet build -c Release

    # info "Copying config files..."
    # cp -vf $CONFIG_DIR/*.* $BIN_DIR/
    # cp -vf $SCRIPT_DIR/configs/test-runner.json $BIN_DIR/SpecFlowPlusRunner/net5.0/appsettings.json
    # cp -vf $BIN_DIR/libe_sqlite3.so $BIN_DIR/SpecFlowPlusRunner/net5.0/
    popd
}

function start_services() {
    info "Starting dependencies docker-compose $LOADDEV up -d --force-recreate..."
    docker-compose $LOADDEV up -d --force-recreate


    HOST_IP=$(docker network inspect testrunner | jq .[0].IPAM.Config[0].Gateway)
    HOST_IP=$(sed -e 's/^"//' -e 's/"$//' <<<"$HOST_IP")
    info "Host IP = $HOST_IP"
    export HOST_IP

    set +e
    while true; do
        info "Waiting for Informatics Gateway..."
        count=$(curl -s http://$HOST_IP:5000/health/status | jq | grep "Running" | wc -l)
        info "$count services running..."
        if [ $count -eq 6 ]; then
            break
        fi
        sleep 1s
    done
    set -e

    sleep 1
    sudo chown -R $USER:$USER $RUN_DIR
}

function write_da_metrics() {
    CID="$(docker container list | grep integrationtest_informatics-gateway | awk '{{print $1}}')"
    info "Streaming Informatics Gateway perf logs from container $CID to $METRICSFILE"

    until $EXIT; do
        DATA=$(docker stats $CID  --no-stream --format "`date +%s`,{{.CPUPerc}},{{.MemUsage}},{{.NetIO}},{{.BlockIO}}")
        echo $DATA >> $METRICSFILE
        sleep 1
    done
}

function stream_da_metrics() {
    [ -f $METRICSFILE ] && sudo rm $METRICSFILE
    write_da_metrics &
}

function run_test() {
    pushd $TEST_DIR
    set +e
    info "Starting test runner..."
    dotnet test -c Release
    EXITCODE=$?
    EXIT=true
    set -e
    popd
}

function generate_reports() {
    set +e
    info "Generating livingdoc..."
    pushd $BIN_DIR
    [ -f $BIN_DIR/TestExecution.json ] && mv $BIN_DIR/TestExecution.json $BIN_DIR/TestExecution_$(date +"%Y_%m_%d_%I_%M_%p").json
    livingdoc test-assembly Monai.Deploy.InformaticsGateway.Integration.Test.dll -t TestExecution*.json -o $SCRIPT_DIR/
    popd
    set -e
}

function save_logs() {
    info "Saving service log..."
    docker-compose logs --no-color -t | sort -u -k 3  > "services.log"
}

function tear_down() {
    info "Stopping services..."
    docker-compose down --remove-orphans
}

function main() {
    env_setup "$@"
    build
    start_services
    stream_da_metrics
    run_test
    generate_reports
    save_logs
    tear_down
    exit $EXITCODE
}

main "$@"
