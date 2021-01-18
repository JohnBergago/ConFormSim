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
mkdir -p $PROJ_DIR/src/ConFormSimProject/Assets/ConFormSim/External/ml-imagesynthesis
cp -r $PROJ_DIR/src/ml-imagesynthesis/Assets/ImageSynthesis/* $PROJ_DIR/src/ConFormSimProject/Assets/ConFormSim/External/ml-imagesynthesis/
cp $PROJ_DIR/src/ml-imagesynthesis/LICENSE.TXT $PROJ_DIR/src/ConFormSimProject/Assets/ConFormSim/External/ml-imagesynthesis/

echo -e "# ConFormSim setup done. Don't forget to download serialized dictionary"
echo -e "# from the Asset Store. For more information read the docs.\n"
