param([Parameter(Mandatory=$true)][string]$Jdk,[string]$OutputDirectory=(Join-Path $PSScriptRoot '../artifacts/android-tests'))
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $output | Out-Null
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
& "$Jdk/bin/javac.exe" -encoding UTF-8 --release 8 -d $output "$root/android/app/src/main/java/br/com/lume/Protocol.java" "$root/android/app/src/main/java/br/com/lume/PacketIO.java" "$root/android/app/src/main/java/br/com/lume/Discovery.java" "$root/android/tests/ProtocolTest.java"
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar testes Java.'}
& "$Jdk/bin/java.exe" -cp $output br.com.lume.ProtocolTest
if($LASTEXITCODE -ne 0){throw 'Falha nos testes de protocolo/UDP.'}
node "$root/android/tests/editor-test.cjs"
if($LASTEXITCODE -ne 0){throw 'Falha nos testes do editor.'}
