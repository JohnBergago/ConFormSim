#!/bin/bash

CONFORMSIM_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# check if file with path to unity exists
if [ -f $CONFORMSIM_DIR/.unitypath ]; then
    echo "Found unity path"
    UNITY_PATH=$(head -n 1 $CONFORMSIM_DIR/.unitypath)
else
    echo "Couldn't find .unitypath file. Please install Unity according to the docs 
and  specify path to Unity (e.g. /opt/Unity/2019.2.12f1/Editor/Unity)."
    read -e -p "Enter path to Unity: " UNITY_PATH
    echo "$UNITY_PATH" > $CONFORMSIM_DIR/.unitypath
    echo "Restart the build script to compile all environments."
    exit 1
fi

if [ -f $UNITY_PATH ]; then
    # build all unity projects in src folder
    for f in $CONFORMSIM_DIR/src/ConForm*; do
        if [ -d "$f" ]; then
            $UNITY_PATH -batchmode -nographics -quit\
            -projectPath "$f" -executeMethod BuildFromCommandLine.PerformBuild
            echo "Done with $f"
        fi
    done
else
    echo "Invalid Unity path $UNITY_PATH. Please rerun the build script and enter a 
    new path."
    rm $CONFORMSIM_DIR/.unitypath
fi 


