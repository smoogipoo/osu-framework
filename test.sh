#!/bin/bash

DEADLOCK_TIME=900

UPTIME=$(uptime -p)
echo "Uptime: $UPTIME"

rm -rf inspectcode
dotnet tool restore

dotnet tool install -g dotnet-dump

dotnet jb inspectcode $(pwd)/osu-framework.Desktop.slnf --debug --loglevel=TRACE --logfile=$(pwd)/inspectcode.log --output=$(pwd)/inspectcodereport.xml --cachesDir=$(pwd)/inspectcode &

echo "Waiting for R# CLI"
sleep 10

PID=$(ps axf | grep 'dotnet exec' | grep -v grep | awk '{print $1}')
echo "R# PID: $PID"

echo "Waiting $DEADLOCK_TIME seconds for deadlock"
sleep $DEADLOCK_TIME

if ! ps -p $PID > /dev/null; then
    echo "Process finished, aborting."
    exit 1
fi

echo "Deadlocked, dumping..."
dotnet dump collect -p $PID -o deadlock.dmp

echo "Compressing dump..."
tar -cjSf deadlock.tar.bz2 deadlock.dmp

echo "Compressing log..."
tar -cjSf inspectcode.log.tar.bz2 $(pwd)/inspectcode.log

echo "Killing R# process"
kill $PID
