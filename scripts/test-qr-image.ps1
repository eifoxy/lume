param([Parameter(Mandatory=$true)][string]$Jdk,[Parameter(Mandatory=$true)][string]$AppDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$image=Join-Path ([IO.Path]::GetFullPath($AppDirectory)) 'qr-fixture.png'
$output=Join-Path $root 'artifacts/qr-image-test'
New-Item -ItemType Directory -Force $output|Out-Null
$jar=Join-Path $output 'zxing-core-3.5.3.jar'
if(!(Test-Path -LiteralPath $jar)){Invoke-WebRequest -Uri 'https://repo.maven.apache.org/maven2/com/google/zxing/core/3.5.3/core-3.5.3.jar' -OutFile $jar}
if((Get-FileHash -LiteralPath $jar -Algorithm SHA256).Hash -ne '8D8064C1636FDAEF7189DD9055C7D59950A8940A12F2293956446EC3C109FD82'){throw 'Invalid QR reader dependency.'}
& "$Jdk/bin/javac.exe" -encoding UTF-8 --release 8 -cp $jar -d $output "$root/android/tests/QrImageTest.java"
if($LASTEXITCODE -ne 0){throw 'QR image test compilation failed.'}
& "$Jdk/bin/java.exe" -cp "$output;$jar" br.com.lume.QrImageTest $image
if($LASTEXITCODE -ne 0){throw 'Windows QR could not be decoded with the Android engine.'}
