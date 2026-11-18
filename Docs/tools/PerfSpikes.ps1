# Week 16 - what the slow frames actually are.
#
# PerfStats.ps1 says the mean is fine and the p95 is not. This script answers the next question: are
# the slow frames scattered, periodic, or lined up with something the game does on a timer (a match
# load, a repath, a decision tick)? It prints an interval histogram, every frame over the threshold
# with its segment and elapsed time, and the spacing between those frames - a repeating gap means a
# repeating cause, which is what a bottleneck has to look like before anything is "optimised".
#
#   powershell -NoProfile -File Docs\tools\PerfSpikes.ps1 -Run "perf_baseline_run1_20261118_223505"

param(
    [string]$Run = 'perf_baseline_run1_20261118_223505',
    [string]$Folder = 'Docs\Zedrich Abrera\Week_16\Evidence\raw_csv',
    [double]$Threshold = 25.0,
    [int]$Bucket = 2
)

$file = Join-Path $Folder ($Run + '.csv')
if (-not (Test-Path $file)) { Write-Output "no such CSV: $file"; exit 1 }

$lines = Get-Content $file
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
$segOf = New-Object System.Collections.Generic.List[int]
$elapsedOf = New-Object System.Collections.Generic.List[double]
$frameOf = New-Object System.Collections.Generic.List[int]
$mainOf = New-Object System.Collections.Generic.List[double]

foreach ($r in $rows) {
    $interval.Add([double]$r[$col['interval_ms']])
    $mainOf.Add([double]$r[$col['main_thread_ms']])
    $segOf.Add([int]$r[$col['segment']])
    $elapsedOf.Add([double]$r[$col['elapsed_s']])
    $frameOf.Add([int]$r[$col['frame']])
}

Write-Output "run: $Run   frames: $($interval.Count)"
Write-Output ""

Write-Output "interval histogram (bucket ${Bucket} ms):"
$buckets = @{}
foreach ($v in $interval) {
    $k = [Math]::Floor($v / $Bucket) * $Bucket
    if (-not $buckets.ContainsKey($k)) { $buckets[$k] = 0 }
    $buckets[$k]++
}
foreach ($k in ($buckets.Keys | Sort-Object)) {
    $share = 100.0 * $buckets[$k] / $interval.Count
    if ($share -lt 0.05) { continue }
    Write-Output ("  {0,6:F0} ms  {1,7} frames  {2,5:F1} %" -f $k, $buckets[$k], $share)
}
Write-Output ""

$slow = New-Object System.Collections.Generic.List[int]
for ($i = 0; $i -lt $interval.Count; $i++) { if ($interval[$i] -gt $Threshold) { $slow.Add($i) } }

$share = 100.0 * $slow.Count / $interval.Count
Write-Output ("frames over {0} ms: {1} ({2:F1} %)" -f $Threshold, $slow.Count, $share)
Write-Output ""

if ($slow.Count -gt 0) {
    Write-Output "spacing between those frames (a repeating gap points at a repeating cause):"
    $gaps = New-Object System.Collections.Generic.List[int]
    for ($i = 1; $i -lt $slow.Count; $i++) { $gaps.Add($slow[$i] - $slow[$i - 1]) }
    $gaps | Group-Object | Sort-Object Count -Descending | Select-Object -First 6 | ForEach-Object {
        Write-Output ("  gap {0,4} frames  x{1}" -f $_.Name, $_.Count)
    }
    Write-Output ""

    Write-Output "first 25 slow frames (season: segment, frame, elapsed_s, interval_ms, main_thread_ms):"
    $limit = [Math]::Min(25, $slow.Count)
    for ($i = 0; $i -lt $limit; $i++) {
        $n = $slow[$i]
        Write-Output ("  seg {0}  frame {1,6}  t {2,6:F2} s  interval {3,7:F2}  main {4,7:F2}" -f `
            $segOf[$n], $frameOf[$n], $elapsedOf[$n], $interval[$n], $mainOf[$n])
    }
    Write-Output ""

    # How many slow frames sit at the start of a segment, i.e. right after a scene reload?
    $atSegmentStart = 0
    for ($i = 0; $i -lt $slow.Count; $i++) {
        $n = $slow[$i]
        if ($n -lt 5) { $atSegmentStart++; continue }
        if ($segOf[$n] -ne $segOf[$n - 5]) { $atSegmentStart++ }
    }
    Write-Output ("slow frames within 5 frames of a segment boundary (a reload): {0} of {1}" -f $atSegmentStart, $slow.Count)
}
