#! /bin/bash
set -e

outputFolder='_output'
testPackageFolder='_tests'
artifactsFolder="_artifacts";

ProgressStart()
{
    echo "Start '$1'"
}

ProgressEnd()
{
    echo "Finish '$1'"
}

UpdateVersionNumber()
{
    if [ "$PROWLARRVERSION" != "" ]; then
        echo "Updating Version Info"
        sed -i'' -e "s/<AssemblyVersion>[0-9.*]\+<\/AssemblyVersion>/<AssemblyVersion>$PROWLARRVERSION<\/AssemblyVersion>/g" src/Directory.Build.props
        sed -i'' -e "s/<AssemblyConfiguration>[\$()A-Za-z-]\+<\/AssemblyConfiguration>/<AssemblyConfiguration>${BUILD_SOURCEBRANCHNAME}<\/AssemblyConfiguration>/g" src/Directory.Build.props
        sed -i'' -e "s/<string>10.0.0.0<\/string>/<string>$PROWLARRVERSION<\/string>/g" distribution/osx/Prowlarr.app/Contents/Info.plist
    fi
}

LintUI()
{
    ProgressStart 'ESLint'
    yarn lint
    ProgressEnd 'ESLint'

    ProgressStart 'Stylelint'
    if [ "$os" = "windows" ]; then
        yarn stylelint-windows
    else
        yarn stylelint-linux
    fi
    ProgressEnd 'Stylelint'
}

Build()
{
    ProgressStart 'Build'

    rm -rf $outputFolder
    rm -rf $testPackageFolder

    slnFile=src/Prowlarr.sln

    if [ $os = "windows" ]; then
        platform=Windows
    else
        platform=Posix
    fi

    dotnet clean $slnFile -c Debug
    dotnet clean $slnFile -c Release

    if [[ -z "$RID" || -z "$FRAMEWORK" ]];
    then
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform=$platform -t:PublishAllRids
    else
        dotnet msbuild -restore $slnFile -p:Configuration=Release -p:Platform=$platform -p:RuntimeIdentifiers=$RID -t:PublishAllRids
    fi

    ProgressEnd 'Build'
}

YarnInstall()
{
    ProgressStart 'yarn install'
    yarn install --frozen-lockfile --network-timeout 120000
    ProgressEnd 'yarn install'
}

RunGulp()
{
    ProgressStart 'Running gulp'
    yarn run build --production
    ProgressEnd 'Running gulp'
}

PackageFiles()
{
    local folder="$1"
    local framework="$2"
    local runtime="$3"

    rm -rf $folder
    mkdir -p $folder
    cp -r $outputFolder/$framework/$runtime/publish/* $folder
    cp -r $outputFolder/Prowlarr.Update/$framework/$runtime/publish $folder/Prowlarr.Update
    cp -r $outputFolder/UI $folder

    echo "Adding LICENSE"
    cp LICENSE $folder
}

PackageLinux()
{
    local framework="$1"
    local runtime="$2"

    ProgressStart "Creating $runtime Package for $framework"

    local folder=$artifactsFolder/$runtime/$framework/Prowlarr

    PackageFiles "$folder" "$framework" "$runtime"

    echo "Removing Service helpers"
    rm -f $folder/ServiceUninstall.*
    rm -f $folder/ServiceInstall.*

    echo "Removing Prowlarr.Windows"
    rm $folder/Prowlarr.Windows.*

    echo "Adding Prowlarr.Mono to UpdatePackage"
    cp $folder/Prowlarr.Mono.* $folder/Prowlarr.Update
    if [ "$framework" = "net5.0" ]; then
        cp $folder/Mono.Posix.NETStandard.* $folder/Prowlarr.Update
        cp $folder/libMonoPosixHelper.* $folder/Prowlarr.Update
    fi

    ProgressEnd "Creating $runtime Package for $framework"
}

PackageMacOS()
{
    local framework="$1"
    
    ProgressStart "Creating MacOS Package for $framework"

    local folder=$artifactsFolder/macos/$framework/Prowlarr

    PackageFiles "$folder" "$framework" "osx-x64"

    echo "Removing Service helpers"
    rm -f $folder/ServiceUninstall.*
    rm -f $folder/ServiceInstall.*

    echo "Removing Prowlarr.Windows"
    rm $folder/Prowlarr.Windows.*

    echo "Adding Prowlarr.Mono to UpdatePackage"
    cp $folder/Prowlarr.Mono.* $folder/Prowlarr.Update
    if [ "$framework" = "net5.0" ]; then
        cp $folder/Mono.Posix.NETStandard.* $folder/Prowlarr.Update
        cp $folder/libMonoPosixHelper.* $folder/Prowlarr.Update
    fi

    ProgressEnd 'Creating MacOS Package'
}

PackageMacOSApp()
{
    local framework="$1"
    
    ProgressStart "Creating macOS App Package for $framework"

    local folder=$artifactsFolder/macos-app/$framework

    rm -rf $folder
    mkdir -p $folder
    cp -r distribution/osx/Prowlarr.app $folder
    mkdir -p $folder/Prowlarr.app/Contents/MacOS

    echo "Copying Binaries"
    cp -r $artifactsFolder/macos/$framework/Prowlarr/* $folder/Prowlarr.app/Contents/MacOS

    echo "Removing Update Folder"
    rm -r $folder/Prowlarr.app/Contents/MacOS/Prowlarr.Update

    ProgressEnd 'Creating macOS App Package'
}

PackageWindows()
{
    local framework="$1"
    
    ProgressStart "Creating Windows Package for $framework"

    local folder=$artifactsFolder/windows/$framework/Prowlarr
    
    PackageFiles "$folder" "$framework" "win-x64"
    cp -r $outputFolder/$framework-windows/$runtime/publish/* $folder

    echo "Removing Prowlarr.Mono"
    rm -f $folder/Prowlarr.Mono.*
    rm -f $folder/Mono.Posix.NETStandard.*
    rm -f $folder/libMonoPosixHelper.*

    echo "Adding Prowlarr.Windows to UpdatePackage"
    cp $folder/Prowlarr.Windows.* $folder/Prowlarr.Update

    ProgressEnd 'Creating Windows Package'
}

Package()
{
    local framework="$1"
    local runtime="$2"
    local SPLIT

    IFS='-' read -ra SPLIT <<< "$runtime"

    case "${SPLIT[0]}" in
        linux)
            PackageLinux "$framework" "$runtime"
            ;;
        win)
            PackageWindows "$framework"
            ;;
        osx)
            PackageMacOS "$framework"
            PackageMacOSApp "$framework"
            ;;
    esac
}

PackageTests()
{
    local framework="$1"
    local runtime="$2"

    cp test.sh "$testPackageFolder/$framework/$runtime/publish"

    rm -f $testPackageFolder/$framework/$runtime/*.log.config

    # geckodriver.exe isn't copied by dotnet publish
    if [ "$runtime" = "win-x64" ];
    then
        curl -Lso gecko.zip "https://github.com/mozilla/geckodriver/releases/download/v0.27.0/geckodriver-v0.27.0-win64.zip"
        unzip -o gecko.zip
        cp geckodriver.exe "$testPackageFolder/$framework/win-x64/publish"
    fi

    ProgressEnd 'Creating Test Package'
}

# Use mono or .net depending on OS
case "$(uname -s)" in
    CYGWIN*|MINGW32*|MINGW64*|MSYS*)
        # on windows, use dotnet
        os="windows"
        ;;
    *)
        # otherwise use mono
        os="posix"
        ;;
esac

POSITIONAL=()

if [ $# -eq 0 ]; then
    echo "No arguments provided, building everything"
    BACKEND=YES
    FRONTEND=YES
    PACKAGES=YES
    LINT=YES
fi

while [[ $# -gt 0 ]]
do
key="$1"

case $key in
    --backend)
        BACKEND=YES
        shift # past argument
        ;;
    -r|--runtime)
        RID="$2"
        shift # past argument
        shift # past value
        ;;
    -f|--framework)
        FRAMEWORK="$2"
        shift # past argument
        shift # past value
        ;;
    --frontend)
        FRONTEND=YES
        shift # past argument
        ;;
    --packages)
        PACKAGES=YES
        shift # past argument
        ;;
    --lint)
        LINT=YES
        shift # past argument
        ;;
    --all)
        BACKEND=YES
        FRONTEND=YES
        PACKAGES=YES
        LINT=YES
        shift # past argument
        ;;
    *)    # unknown option
        POSITIONAL+=("$1") # save it in an array for later
        shift # past argument
        ;;
esac
done
set -- "${POSITIONAL[@]}" # restore positional parameters

if [ "$BACKEND" = "YES" ];
then
    UpdateVersionNumber
    Build
    if [[ -z "$RID" || -z "$FRAMEWORK" ]];
    then
        PackageTests "net5.0" "win-x64"
        PackageTests "net5.0" "linux-x64"
        PackageTests "net5.0" "linux-musl-x64"
        PackageTests "net5.0" "osx-x64"
    else
        PackageTests "$FRAMEWORK" "$RID"
    fi
fi

if [ "$FRONTEND" = "YES" ];
then
    YarnInstall
    RunGulp
fi

if [ "$LINT" = "YES" ];
then
    if [ -z "$FRONTEND" ];
    then
        YarnInstall
    fi
    
    LintUI
fi

if [ "$PACKAGES" = "YES" ];
then
    UpdateVersionNumber

    if [[ -z "$RID" || -z "$FRAMEWORK" ]];
    then
        Package "net5.0" "win-x64"
        Package "net5.0" "linux-x64"
        Package "net5.0" "linux-musl-x64"
        Package "net5.0" "linux-arm64"
        Package "net5.0" "linux-musl-arm64"
        Package "net5.0" "linux-arm"
        Package "net5.0" "osx-x64"
    else
        Package "$FRAMEWORK" "$RID"
    fi
fi
