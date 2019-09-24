#!/bin/bash

res=0;

while :
do
    dotnet run --configuration Release -- $res ;
    res=$?
    if [ $res -eq 88 ]
    then
        echo "%die command was recieved, exited successfully";
        break;
    elif [ $res -eq 42 ]
    then
        echo "%restart command was recieved, updating";
        git pull;
        res=$?
        dotnet build --configuration Release;
    else
        echo "bot crashed, restarting";
    fi
done