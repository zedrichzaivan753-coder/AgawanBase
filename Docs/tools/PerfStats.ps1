# Week 16 - turn PerfCapture CSVs into the numbers the documents quote.
#
# Every figure printed here comes from the raw per-frame rows; nothing is estimated. Run it once per
# configuration (baseline / fix1 / fix2 / final) and paste the table straight into SA6 or AS3.
#
#   powershell -NoProfile -File Docs\tools\PerfStats.ps1 -Pattern "perf_baseline*.csv"
#
# Columns out: run, frames, live_s, segments, interval avg/p95/worst ms, fps, main avg/p95/worst ms,
# gc bytes/frame, draw calls avg/max, triangles max, memory avg MB.

param(
    [string]$Pattern = 'perf_baseline*.csv',
    [string]$Folder = 'Docs\Zedrich Abrera\Week_16\Evidence\raw_csv'
)

function Percentile($sorted, [double]$fraction) {
    if ($sorted.Count -eq 0) { return 0 }
    $index = [int][Math]::Round($fraction * ($sorted.Count - 1))
    return $sorted[$index]
}

function Mean($values) {
    if ($values.Count -eq 0) { return 0 }
    $sum = 0.0
    foreach ($v in $values) { $sum += $v }
    return $sum / $values.Count
}

$files = Get-ChildItem -Path $Folder -Filter $Pattern | Sort-Object Name
if ($files.Count -eq 0) { Write-Output "no CSVs matched '$Pattern' in '$Folder'"; exit 1 }

Write-Output "| run | frames | live s | seg | interval avg | p95 | worst | fps | main avg | p95 | worst | GC B/frame | draws avg | draws max | tris max | mem avg MB |"
Write-Output "|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|"

$collected = @{ avgInterval = @(); p95Interval = @(); avgMain = @(); p95Main = @(); avgGc = @(); avgDraws = @() }

foreach ($file in ($files | Sort-Object Name)) {
    $lines = Get-Content $file.FullName
    $header = $null
    $rows = New-Object System.Collections.Generic.List[object]

    foreach ($line in $lines) {
        if ($line.StartsWith('#')) { continue }
        if ($null -eq $header) { $header = $line.Split(','); continue }
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $rows.Add($line.Split(','))
    }

    $col = @{}
    for ($i = 0; $i -lt $header.Count; $i++) { $col[$header[$i]] = $i }

    $interval = New-Object System.Collections.Generic.List[double]
    $main = New-Object System.Collections.Generic.List[double]
    $gc = New-Object System.Collections.Generic.List[double]
    $draws = New-Object System.Collections.Generic.List[double]
    $tris = New-Object System.Collections.Generic.List[double]
    $mem = New-Object System.Collections.Generic.List[double]
    $segments = New-Object System.Collections.Generic.HashSet[int]
    $live = 0.0

    foreach ($r in $rows) {
        $interval.Add([double]$r[$col['interval_ms']])
        $main.Add([double]$r[$col['main_thread_ms']])
        $g = [double]$r[$col['gc_bytes']]; if ($g -ge 0) { $gc.Add($g) }
        $d = [double]$r[$col['draw_calls']]; if ($d -gt 0) { $draws.Add($d) }
        $t = [double]$r[$col['triangles']]; if ($t -gt 0) { $tris.Add($t) }
        $m = [double]$r[$col['total_memory_mb']]; if ($m -gt 0) { $mem.Add($m) }
        [void]$segments.Add([int]$r[$col['segment']])
        $live += [double]$r[$col['interval_ms']] / 1000.0
    }

    $interval.Sort()
    $main.Sort()

    $avgInterval = Mean $interval
    $p95Interval = Percentile $interval 0.95
    $worstInterval = $interval[$interval.Count - 1]
    $avgMain = Mean $main
    $p95Main = Percentile $main 0.95
    $worstMain = $main[$main.Count - 1]
    $avgGc = Mean $gc
    $avgDraws = Mean $draws
    $maxDraws = -1
    if ($draws.Count -gt 0) { $maxDraws = ($draws | Measure-Object -Maximum).Maximum }
    $maxTris = -1
    if ($tris.Count -gt 0) { $maxTris = ($tris | Measure-Object -Maximum).Maximum }
    $avgMem = Mean $mem
    $fps = 0.0
    if ($avgInterval -gt 0) { $fps = 1000.0 / $avgInterval }

    Write-Output ("| {0} | {1} | {2:F1} | {3} | {4:F2} | {5:F2} | {6:F2} | {7:F0} | {8:F2} | {9:F2} | {10:F2} | {11:F0} | {12:F1} | {13:F0} | {14:F0} | {15:F0} |" -f `
        $file.BaseName, $rows.Count, $live, $segments.Count, $avgInterval, $p95Interval, $worstInterval, `
        $fps, $avgMain, $p95Main, $worstMain, $avgGc, $avgDraws, $maxDraws, $maxTris, $avgMem)

    $collected.avgInterval += $avgInterval
    $collected.p95Interval += $p95Interval
    $collected.avgMain += $avgMain
    $collected.p95Main += $p95Main
    $collected.avgGc += $avgGc
    $collected.avgDraws += $avgDraws
}

Write-Output ""
Write-Output "Across $($files.Count) run(s) - per-run values and their spread (the noise band):"
foreach ($key in @('avgInterval', 'p95Interval', 'avgMain', 'p95Main', 'avgGc', 'avgDraws')) {
    $list = $collected[$key]
    if ($null -eq $list -or $list.Count -eq 0) { continue }
    $mean = Mean $list
    $min = ($list | Measure-Object -Minimum).Minimum
    $max = ($list | Measure-Object -Maximum).Maximum
    Write-Output ("  {0,-12} mean {1,10:F2}   min {2,10:F2}   max {3,10:F2}   spread {4,8:F2}" -f $key, $mean, $min, $max, ($max - $min))
}
