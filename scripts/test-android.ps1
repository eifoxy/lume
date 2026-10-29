param([Parameter(Mandatory=$true)][string]$Jdk,[string]$OutputDirectory=(Join-Path $PSScriptRoot '../artifacts/android-tests'))
$ErrorActionPreference='Stop'
$output=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $output | Out-Null
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
node "$root/scripts/sync-localization.cjs" --check
Push-Location $root
try { node scripts/test-qr.cjs; if($LASTEXITCODE -ne 0){throw 'Falha no protocolo QR.'} } finally { Pop-Location }
if($LASTEXITCODE -ne 0){throw 'Catálogo de idiomas fora de sincronia.'}
& "$Jdk/bin/javac.exe" -encoding UTF-8 --release 8 -d $output "$root/android/app/src/main/java/br/com/lume/Protocol.java" "$root/android/app/src/main/java/br/com/lume/PacketIO.java" "$root/android/app/src/main/java/br/com/lume/Discovery.java" "$root/android/app/src/main/java/br/com/lume/Upload.java" "$root/android/app/src/main/java/br/com/lume/Preview.java" "$root/android/tests/ProtocolTest.java" "$root/android/tests/SmartTimerTest.java" "$root/android/tests/PreviewTest.java"
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar testes Java.'}
& "$Jdk/bin/java.exe" -cp $output br.com.lume.ProtocolTest
if($LASTEXITCODE -ne 0){throw 'Falha nos testes de protocolo/UDP.'}
& "$Jdk/bin/java.exe" -cp $output br.com.lume.SmartTimerTest
if($LASTEXITCODE -ne 0){throw 'Falha nos testes Smart WiFi Timer.'}
& "$Jdk/bin/java.exe" -cp $output br.com.lume.PreviewTest
if($LASTEXITCODE -ne 0){throw 'Falha nos testes de prévia ao vivo.'}
node "$root/android/tests/editor-test.cjs"
if($LASTEXITCODE -ne 0){throw 'Falha nos testes do editor.'}
node "$root/android/tests/auto-discovery-test.cjs"
if($LASTEXITCODE -ne 0){throw 'Falha nos testes de busca automática.'}
node "$root/android/tests/i18n-test.cjs"
if($LASTEXITCODE -ne 0){throw 'Falha nos testes de idiomas.'}
node "$root/android/tests/live-ui-test.cjs"
if($LASTEXITCODE -ne 0){throw 'Falha nos testes da interface de prévia ao vivo.'}
