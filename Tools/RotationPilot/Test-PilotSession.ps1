param([Parameter(Mandatory=$true)][string]$SessionPath,[switch]$AllowSynthetic)
$ErrorActionPreference='Stop'
$p4Directory=(Resolve-Path -LiteralPath $SessionPath).Path
$p4Metadata=Get-Content -LiteralPath (Join-Path $p4Directory 'session.json') -Raw | ConvertFrom-Json
$p4Status=Get-Content -LiteralPath (Join-Path $p4Directory 'session_status.json') -Raw | ConvertFrom-Json
$p4Rows=@(Get-Content -LiteralPath (Join-Path $p4Directory 'results.txt') | Where-Object {$_ -ne ''})
$p4Issues=[System.Collections.Generic.List[string]]::new()
if($p4Metadata.softwareSimulation -and -not $AllowSynthetic) {$p4Issues.Add('Synthetic output is not a participant pilot dataset.')}
if([string]::IsNullOrWhiteSpace($p4Metadata.scaleInterpretation)) {$p4Issues.Add('The response instructions used for the session are missing.')}
if([string]::IsNullOrWhiteSpace($p4Metadata.participantId)) {$p4Issues.Add('Participant ID is missing.')}
if($p4Rows.Count -ne $p4Metadata.expectedMeasuredTrials) {$p4Issues.Add('Measured row count does not match the configured trial count.')}
if(-not $p4Status.complete -or $p4Status.completedMeasuredTrials -ne $p4Metadata.expectedMeasuredTrials) {$p4Issues.Add('Session is incomplete.')}
$p4Conditions=[System.Collections.Generic.Dictionary[string,int]]::new()
foreach($p4Row in $p4Rows) {
    $p4Fields=$p4Row -split "`t"
    if($p4Fields.Count -ne 4) {$p4Issues.Add('A result row does not contain four fields.'); continue}
    $p4Response=0
    if(-not [int]::TryParse($p4Fields[3],[ref]$p4Response) -or $p4Response -lt -3 -or $p4Response -gt 3) {$p4Issues.Add('A response is outside -3 to +3.')}
    $p4Key=$p4Fields[0]+':'+$p4Fields[1]
    if(-not $p4Conditions.ContainsKey($p4Key)) {$p4Conditions[$p4Key]=0}
    $p4Conditions[$p4Key]++
}
if($p4Conditions.Count -ne ($p4Metadata.steps * $p4Metadata.steps2)) {$p4Issues.Add('Not all configured gain conditions are present.')}
foreach($p4Count in $p4Conditions.Values) {if($p4Count -ne $p4Metadata.repetitions) {$p4Issues.Add('A gain condition has the wrong repetition count.')}}
foreach($p4Name in @('motion.tsv','events.tsv','data.txt')) {
    $p4File=Get-Item -LiteralPath (Join-Path $p4Directory $p4Name) -ErrorAction SilentlyContinue
    if($null -eq $p4File -or $p4File.Length -eq 0) {$p4Issues.Add("Missing or empty $p4Name")}
}
[pscustomobject]@{
    ParticipantId=$p4Metadata.participantId
    Experiment=$p4Metadata.experiment
    Rows=$p4Rows.Count
    Conditions=$p4Conditions.Count
    Synthetic=[bool]$p4Metadata.softwareSimulation
    SoftwareChecksPassed=($p4Issues.Count -eq 0)
    ActualPilotComplete=($p4Issues.Count -eq 0 -and -not $p4Metadata.softwareSimulation)
    ProtocolConfirmed=[bool]$p4Metadata.scaleInterpretationConfirmed
    ProtocolStatus=$p4Metadata.protocolStatus
    Issues=$p4Issues.ToArray()
}
if($p4Issues.Count -gt 0) {throw ($p4Issues -join ' ')}
