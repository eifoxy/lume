param([string]$OutputDirectory=(Join-Path $PSScriptRoot '../artifacts'))
$ErrorActionPreference='Stop'
$project=Join-Path $PSScriptRoot '../windows/src/Lume.csproj'
$output=[IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force $output | Out-Null
dotnet publish $project -c Release --disable-build-servers -p:UseSharedCompilation=false --self-contained false -o "$output/windows-portable"
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar Windows portátil.'}
dotnet publish $project -c Release --disable-build-servers -p:UseSharedCompilation=false -r win-x64 --self-contained true -p:RuntimeFrameworkVersion=8.0.32 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$output/windows-x64"
if($LASTEXITCODE -ne 0){throw 'Falha ao compilar Windows x64.'}
foreach($folder in @('windows-portable','windows-x64')){
 $guide=[IO.File]::ReadAllText((Join-Path $PSScriptRoot '../docs/INSTALACAO-WINDOWS.md'))
 $guide=$guide.Replace('(../README.md)','(https://github.com/eifoxy/lume)').Replace('(WIFI.md','(https://github.com/eifoxy/lume/blob/main/docs/WIFI.md').Replace('(USO.md','(https://github.com/eifoxy/lume/blob/main/docs/USO.md').Replace('(COMPATIBILIDADE.md','(https://github.com/eifoxy/lume/blob/main/docs/COMPATIBILIDADE.md').Replace('(images/previa-wifi.png)','(https://raw.githubusercontent.com/eifoxy/lume/main/docs/images/previa-wifi.png)')
 [IO.File]::WriteAllText("$output/$folder/LEIA-ME.md",$guide)
 Copy-Item (Join-Path $PSScriptRoot '../LICENSE') "$output/$folder/LICENSE.txt"
 $items=Get-ChildItem "$output/$folder" -File | Where-Object { $_.Extension -in @('.exe','.dll') -or $_.Name -in @('Lume.deps.json','Lume.runtimeconfig.json','LEIA-ME.md','LICENSE.txt') }
 $suffix=if($folder -eq 'windows-x64'){'win-x64'}else{'portable'}
 Compress-Archive -LiteralPath $items.FullName -DestinationPath "$output/Lume-Windows-0.6.0-$suffix.zip" -Force
}
