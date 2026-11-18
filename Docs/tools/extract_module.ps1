$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$docx = 'C:\Users\Zedrich\AgawanBase\Docs\MAD2-Laboratory-Module-Week-13-18-Individual-Thematic-Mobile-Game-Weekly-Task-Submission.docx'
$out  = 'C:\Users\Zedrich\AgawanBase\Docs\tools\_module_text.txt'

$zip    = [System.IO.Compression.ZipFile]::OpenRead($docx)
$entry  = $zip.GetEntry('word/document.xml')
$reader = New-Object System.IO.StreamReader($entry.Open())
$xml    = $reader.ReadToEnd()
$reader.Close()
$zip.Dispose()

$t = $xml -replace '</w:p>', "`n"
$t = $t -replace '<[^>]+>', ''
$t = $t -replace '&amp;', '&' -replace '&lt;', '<' -replace '&gt;', '>'
$t = $t -replace '[ \t]+', ' '
$t = $t -replace "(`n\s*){2,}", "`n"

$t | Out-File -FilePath $out -Encoding utf8
$i = $t.IndexOf('Week 15')
$j = $t.IndexOf('Week 16')
Write-Output ("total=" + $t.Length + " week15At=" + $i + " week16At=" + $j)
