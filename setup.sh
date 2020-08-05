#!/bin/bash

PROJ_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

if [[ ! -e '.git' ]]
then
    echo "No git repository found. Initializing file structure and checking out git submodules."
    git init .
    mkdir -p src/
    cd src/
    git submodule add https://bitbucket.org/Unity-Technologies/ml-imagesynthesis.git 
fi

git submodule update --init --recursive

# copy the relevant files from ml-imagesynthesis to ConFormSim
cd src/
mkdir -p ConFormSimProject/Assets/External/ml-imagesynthesis
cp ml-imagesynthesis/Assets/* ConFormSimProject/Assets/External/ml-imagesynthesis/
cp ml-imagesynthesis/LICENSE.TXT ConFormSimProject/Assets/External/ml-imagesynthesis/
