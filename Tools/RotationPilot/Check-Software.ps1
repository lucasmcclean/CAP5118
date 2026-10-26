param([string]$UnityEditorPath=(Join-Path $env:ProgramFiles 'Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe'))
$ErrorActionPreference='Stop'
$checkProject=Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$checkData=Join-Path (Split-Path $UnityEditorPath -Parent) 'Data'
$checkCache=Join-Path $checkProject 'Library/ScriptAssemblies'
$checkMono=Join-Path $checkData 'MonoBleedingEdge/bin/mono.exe'
$checkCsc=Join-Path $checkData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($checkRequired in @($UnityEditorPath,$checkMono,$checkCsc,(Join-Path $checkCache 'Oculus.VR.dll'),(Join-Path $checkCache 'UnityEngine.UI.dll'),(Join-Path $checkCache 'Unity.XR.Management.dll'))) {
    if(-not(Test-Path -LiteralPath $checkRequired)) {throw "Required file missing: $checkRequired. Install the specified Unity editor and let Unity import this project first."}
}
$checkOutput=Join-Path $checkProject ('QA/PublishChecks_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $checkOutput | Out-Null
$checkReferences=@(Get-ChildItem -LiteralPath (Join-Path $checkData 'Managed/UnityEngine') -Filter '*.dll' | ForEach-Object {'-r:'+$_.FullName})
$checkReferences+=('-r:'+(Join-Path $checkData 'NetStandard/ref/2.1.0/netstandard.dll'))
foreach($checkAssembly in @('Oculus.VR.dll','UnityEngine.UI.dll','Unity.XR.Management.dll')) {$checkReferences+=('-r:'+(Join-Path $checkCache $checkAssembly))}
$checkRuntimeSources=@(Get-ChildItem -LiteralPath (Join-Path $checkProject 'Assets/Redirection') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $checkMono $checkCsc -noconfig -nologo -nostdlib+ -target:library ('-out:'+(Join-Path $checkOutput 'RuntimeCompile.dll')) @checkReferences @checkRuntimeSources
if($LASTEXITCODE -ne 0) {throw 'Runtime compilation failed.'}
$checkEditorSources=@(Get-ChildItem -LiteralPath (Join-Path $checkProject 'Assets/Editor') -Filter '*.cs' | ForEach-Object {$_.FullName})
& $checkMono $checkCsc -noconfig -nologo -nostdlib+ -target:library ('-out:'+(Join-Path $checkOutput 'EditorCompile.dll')) @checkReferences ('-r:'+(Join-Path $checkOutput 'RuntimeCompile.dll')) @checkEditorSources
if($LASTEXITCODE -ne 0) {throw 'Editor compilation failed.'}
& $checkMono $checkCsc -nologo ('-out:'+(Join-Path $checkOutput 'RecorderTests.exe')) (Join-Path $checkProject 'Assets/Redirection/PilotSessionRecorder.cs') (Join-Path $checkProject 'Tests/RecorderTests.cs')
if($LASTEXITCODE -ne 0) {throw 'Recorder test compilation failed.'}
& $checkMono (Join-Path $checkOutput 'RecorderTests.exe') (Join-Path $checkOutput 'Recorder')
if($LASTEXITCODE -ne 0) {throw 'Recorder tests failed.'}
Write-Output "Runtime and Editor compilation and recorder tests passed. These are software checks, not a headset test. Output: $checkOutput"
