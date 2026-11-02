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
$p4RowsByCondition=@()
foreach($p4Row in $p4Rows) {
    $p4Fields=$p4Row -split "`t"
    if($p4Fields.Count -ne 4) {$p4Issues.Add('A result row does not contain four fields.'); continue}
    $p4Response=0
    if(-not [int]::TryParse($p4Fields[3],[ref]$p4Response) -or $p4Response -lt -3 -or $p4Response -gt 3) {$p4Issues.Add('A response is outside -3 to +3.')}
    elseif($p4Metadata.responseScale -eq '-3,-2,-1,1,2,3' -and $p4Response -eq 0) {$p4Issues.Add('A response is zero on the six-choice scale.')}
    $p4Key=$p4Fields[0]+':'+$p4Fields[1]
    $p4RowsByCondition += $p4Key
    if(-not $p4Conditions.ContainsKey($p4Key)) {$p4Conditions[$p4Key]=0}
    $p4Conditions[$p4Key]++
}
if($p4Conditions.Count -ne ($p4Metadata.steps * $p4Metadata.steps2)) {$p4Issues.Add('Not all configured gain conditions are present.')}
foreach($p4Count in $p4Conditions.Values) {if($p4Count -ne $p4Metadata.repetitions) {$p4Issues.Add('A gain condition has the wrong repetition count.')}}
if($p4Metadata.directionSchedule -eq 'alternate_left_first_across_practice_and_measured') {
    $p4Events=@(Import-Csv -LiteralPath (Join-Path $p4Directory 'events.tsv') -Delimiter "`t")
    $p4DirectionConditions=[System.Collections.Generic.Dictionary[string,int]]::new()
    for($p4Index=0; $p4Index -lt $p4RowsByCondition.Count; $p4Index++) {
        $p4Cues=@($p4Events | Where-Object { $_.trial -eq "$p4Index" -and $_.event -match '^TURN_DIRECTION_(LEFT|RIGHT)$' })
        $p4Turns=@($p4Events | Where-Object { $_.trial -eq "$p4Index" -and $_.event -match '^TURN_COMPLETED_(LEFT|RIGHT)$' })
        if($p4Cues.Count -ne 1 -or $p4Turns.Count -ne 1) {$p4Issues.Add("Trial $p4Index is missing a unique direction cue or completion event."); continue}
        $p4Direction=$p4Cues[0].event.Substring('TURN_DIRECTION_'.Length)
        $p4Expected=if((($p4Metadata.practiceTrials + $p4Index) % 2) -eq 0) {'LEFT'} else {'RIGHT'}
        if($p4Direction -ne $p4Expected -or $p4Turns[0].event -ne ('TURN_COMPLETED_' + $p4Direction)) {
            $p4Issues.Add("Trial $p4Index has an incorrect direction sequence or completion event.")
        }
        $p4DirectionKey=$p4RowsByCondition[$p4Index]+':'+$p4Direction
        if(-not $p4DirectionConditions.ContainsKey($p4DirectionKey)) {$p4DirectionConditions[$p4DirectionKey]=0}
        $p4DirectionConditions[$p4DirectionKey]++
    }
    if($p4DirectionConditions.Count -ne (2 * $p4Metadata.steps * $p4Metadata.steps2)) {$p4Issues.Add('Not all gain and direction pairs are present.')}
    foreach($p4Count in $p4DirectionConditions.Values) {
        if($p4Count -ne $p4Metadata.repetitionsPerDirection) {$p4Issues.Add('A gain and direction pair has the wrong repetition count.')}
    }
}
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
