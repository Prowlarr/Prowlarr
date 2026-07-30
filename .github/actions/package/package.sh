#!/bin/bash

outputFolder=_output
artifactsFolder=_artifacts
uiFolder="$outputFolder/UI"
framework="${FRAMEWORK:=net8.0}"

rm -rf $artifactsFolder
mkdir -p $artifactsFolder

for runtime in _output/*
do
  name="${runtime##*/}"
  folderName="$runtime/$framework"
  prowlarrFolder="$folderName/Prowlarr"
  archiveName="Prowlarr.$BRANCH.$PROWLARRVERSION.$name"

  if [[ "$name" == 'UI' ]]; then
    continue
  fi
    
  echo "Creating package for $name"

  echo "Copying UI"
  cp -r $uiFolder $prowlarrFolder
  
  echo "Setting permissions"
  find $prowlarrFolder -name "Prowlarr" -exec chmod a+x {} 2>/dev/null || true
  find $prowlarrFolder -name "Prowlarr.Update" -exec chmod a+x {} 2>/dev/null || true
  
  if [[ "$name" == *"osx"* ]]; then
    echo "Creating macOS package"
      
    packageName="$name-app"
    packageFolder="$outputFolder/$packageName"
      
    rm -rf $packageFolder
    mkdir -p $packageFolder
      
    cp -r distribution/osx/Prowlarr.app $packageFolder
    mkdir -p $packageFolder/Prowlarr.app/Contents/MacOS
      
    echo "Copying Binaries"
    cp -r $prowlarrFolder/* $packageFolder/Prowlarr.app/Contents/MacOS
      
    echo "Removing Update Folder"
    rm -rf $packageFolder/Prowlarr.app/Contents/MacOS/Prowlarr.Update
              
    echo "Packaging macOS app Artifact"
    (cd $packageFolder; zip -rq "../../$artifactsFolder/$archiveName-app.zip" ./Prowlarr.app)
  fi

  echo "Packaging Artifact"
  if [[ "$name" == *"linux"* ]] || [[ "$name" == *"osx"* ]] || [[ "$name" == *"freebsd"* ]]; then
    tar -zcf "./$artifactsFolder/$archiveName.tar.gz" -C $folderName Prowlarr
  fi
    
  if [[ "$name" == *"win"* ]]; then
    if [ "$RUNNER_OS" = "Windows" ]; then
      (cd $folderName; 7z a -tzip "../../../$artifactsFolder/$archiveName.zip" ./Prowlarr)
    else
      (cd $folderName; zip -rq "../../../$artifactsFolder/$archiveName.zip" ./Prowlarr)
    fi
  fi
done

# Copy Inno Setup Windows installers if present
if compgen -G "distribution/windows/setup/output/Prowlarr.*.exe" > /dev/null; then
  cp distribution/windows/setup/output/Prowlarr.*.exe _artifacts/
fi
