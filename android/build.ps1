param(
 [Parameter(Mandatory=$true)][string]$Jdk,
 [Parameter(Mandatory=$true)][string]$BuildTools,
 [Parameter(Mandatory=$true)][string]$Platform,
 [Parameter(Mandatory=$true)][string]$SigningKey,
 [string]$BuildDirectory = (Join-Path $PSScriptRoot 'build')
)
$ErrorActionPreference='Stop'
function Run([string]$Exe,[string[]]$Arguments){ & $Exe @Arguments; if($LASTEXITCODE -ne 0){throw "Falha: $Exe ($LASTEXITCODE)"} }
$Jdk=(Resolve-Path -LiteralPath $Jdk).Path
$BuildTools=(Resolve-Path -LiteralPath $BuildTools).Path
$Platform=(Resolve-Path -LiteralPath $Platform).Path
$BuildDirectory=[IO.Path]::GetFullPath($BuildDirectory)
New-Item -ItemType Directory -Force $BuildDirectory,(Join-Path $BuildDirectory 'classes'),(Join-Path $BuildDirectory 'dex') | Out-Null
$sources=(Get-ChildItem (Join-Path $PSScriptRoot 'app/src/main/java/br/com/lume/*.java')).FullName
Run "$Jdk/bin/javac.exe" (@('-encoding','UTF-8','--release','8','-cp',"$Platform/android.jar",'-d',"$BuildDirectory/classes")+$sources)
Run "$Jdk/bin/jar.exe" @("-J-Djava.io.tmpdir=$BuildDirectory",'cf',"$BuildDirectory/classes.jar",'-C',"$BuildDirectory/classes",'.')
Run "$Jdk/bin/java.exe" @('-cp',"$BuildTools/lib/d8.jar",'com.android.tools.r8.D8','--min-api','26','--lib',"$Platform/android.jar",'--output',"$BuildDirectory/dex","$BuildDirectory/classes.jar")
$manifest=[IO.File]::ReadAllText("$PSScriptRoot/app/src/main/AndroidManifest.xml").Replace('<manifest xmlns:','<manifest package="br.com.lume.aquario" xmlns:')
[IO.File]::WriteAllText("$BuildDirectory/AndroidManifest.xml",$manifest)
Run "$BuildTools/aapt2.exe" @('compile','--dir',"$PSScriptRoot/app/src/main/res",'-o',"$BuildDirectory/resources.zip")
Run "$BuildTools/aapt2.exe" @('link','-o',"$BuildDirectory/base.apk",'--manifest',"$BuildDirectory/AndroidManifest.xml",'-I',"$Platform/android.jar",'-A',"$PSScriptRoot/app/src/main/assets",'--min-sdk-version','26','--target-sdk-version','36','--version-code','110','--version-name','1.1.0',"$BuildDirectory/resources.zip")
Add-Type -AssemblyName System.IO.Compression.FileSystem
$apk=[IO.Compression.ZipFile]::Open("$BuildDirectory/base.apk",'Update')
try{[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($apk,"$BuildDirectory/dex/classes.dex",'classes.dex')|Out-Null}finally{$apk.Dispose()}
Run "$BuildTools/zipalign.exe" @('-f','4',"$BuildDirectory/base.apk","$BuildDirectory/aligned.apk")
if(!(Test-Path -LiteralPath $SigningKey)){
 New-Item -ItemType Directory -Force (Split-Path $SigningKey) | Out-Null
 Run "$Jdk/bin/keytool.exe" @('-genkeypair','-keystore',$SigningKey,'-storepass','android','-keypass','android','-alias','lume-local','-dname','CN=Lume Local, OU=Personal, O=Lume, C=BR','-keyalg','RSA','-keysize','3072','-validity','10000')
}
$target=Join-Path $PSScriptRoot 'Lume-Android.apk'
Run "$Jdk/bin/java.exe" @('-jar',"$BuildTools/lib/apksigner.jar",'sign','--ks',$SigningKey,'--ks-key-alias','lume-local','--ks-pass','pass:android','--key-pass','pass:android','--out',$target,"$BuildDirectory/aligned.apk")
Run "$Jdk/bin/java.exe" @('-jar',"$BuildTools/lib/apksigner.jar",'verify','--verbose',$target)
Write-Output "APK: $target"
