param([Parameter(Mandatory=$true)][string]$AppDirectory)
$ErrorActionPreference='Stop'
$app=Join-Path ([IO.Path]::GetFullPath($AppDirectory)) 'Lume.exe'
foreach($mode in @('--self-test','--ui-test')){
 $process=Start-Process -FilePath $app -ArgumentList $mode -PassThru -Wait -WindowStyle Hidden
 if($process.ExitCode -ne 0){throw "Falha $mode. Confira os relatórios na pasta de testes."}
}
Get-Content (Join-Path $AppDirectory 'test-results.txt'),(Join-Path $AppDirectory 'ui-test-results.txt')
