param([Parameter(Mandatory=$true)][string]$SessionPath)
$ErrorActionPreference='Stop'
$p4ExportCheck = & (Join-Path $PSScriptRoot 'Test-PilotSession.ps1') -SessionPath $SessionPath
if(-not $p4ExportCheck.ActualPilotComplete) {throw 'The selected session is not a complete physical pilot.'}
$p4Source=(Resolve-Path -LiteralPath $SessionPath).Path
$p4ExportRoot=Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) 'PilotExports'
New-Item -ItemType Directory -Force -Path $p4ExportRoot | Out-Null
$p4ExportName=Split-Path $p4Source -Leaf
$p4Archive=Join-Path $p4ExportRoot ($p4ExportName+'.zip')
if(Test-Path -LiteralPath $p4Archive) {throw 'This export already exists; existing exports are not overwritten.'}
$p4Files=@('session.json','session_status.json','results.txt','motion.tsv','events.tsv','data.txt') |
    ForEach-Object {Join-Path $p4Source $_}
# Unity keeps its flushed log streams open at Thank You. Copy through shared-read
# streams so exporting does not require stopping XR or modifying the live data.
$p4Snapshot=Join-Path $p4ExportRoot ($p4ExportName+'_files_'+(Get-Date -Format 'yyyyMMdd_HHmmss_fff'))
New-Item -ItemType Directory -Path $p4Snapshot | Out-Null
$p4SnapshotFiles=@()
foreach($p4File in $p4Files) {
    $p4Destination=Join-Path $p4Snapshot (Split-Path $p4File -Leaf)
    $p4Input=$null
    $p4Output=$null
    try {
        $p4Input=[System.IO.File]::Open($p4File,[System.IO.FileMode]::Open,
            [System.IO.FileAccess]::Read,[System.IO.FileShare]::ReadWrite)
        $p4Output=[System.IO.File]::Open($p4Destination,[System.IO.FileMode]::CreateNew,
            [System.IO.FileAccess]::Write,[System.IO.FileShare]::None)
        $p4Input.CopyTo($p4Output)
    } finally {
        if($null -ne $p4Output) {$p4Output.Dispose()}
        if($null -ne $p4Input) {$p4Input.Dispose()}
    }
    $p4SnapshotFiles+=$p4Destination
}
$p4SnapshotCheck = & (Join-Path $PSScriptRoot 'Test-PilotSession.ps1') -SessionPath $p4Snapshot
if(-not $p4SnapshotCheck.ActualPilotComplete) {throw 'The export snapshot did not pass validation.'}
Compress-Archive -LiteralPath $p4SnapshotFiles -DestinationPath $p4Archive
$p4Hash=(Get-FileHash -LiteralPath $p4Archive -Algorithm SHA256).Hash.ToLowerInvariant()
[pscustomobject]@{Archive=$p4Archive;Snapshot=$p4Snapshot;SHA256=$p4Hash;ParticipantId=$p4ExportCheck.ParticipantId;
    Rows=$p4ExportCheck.Rows;Conditions=$p4ExportCheck.Conditions;ProtocolConfirmed=$p4ExportCheck.ProtocolConfirmed;
    ProtocolStatus=$p4ExportCheck.ProtocolStatus;Sent=$false}
