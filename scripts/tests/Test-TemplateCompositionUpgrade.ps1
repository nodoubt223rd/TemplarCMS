$ErrorActionPreference = 'Stop'
$repoPath = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$testPath = Join-Path $repoPath ('.tmp/template-upgrade-tests/' + [guid]::NewGuid().ToString('N'))
$inputPath = Join-Path $testPath 'input'
[void](New-Item -ItemType Directory -Path $inputPath -Force)
$pageId = [guid]::NewGuid().ToString()
$titleId = [guid]::NewGuid().ToString()
$fixture = @{
    id=$pageId; name='Page'; key='page'; baseTemplates=@('item');
    sections=@(@{ id=[guid]::NewGuid().ToString(); name='SEO'; key='seo'; sortOrder=150;
        fields=@(@{id=[guid]::NewGuid().ToString(); name='JSON LD'; key='jsonLd'; fieldType='MultiLineText'},
            @{id=$titleId; name='Title'; key='title'; fieldType='SingleLineText'; isShared=$false; isUnversioned=$true}) })
}
$inputFile = Join-Path $inputPath 'page.json'
$fixture | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $inputFile
$before = (Get-FileHash -LiteralPath $inputFile).Hash
$helper = Join-Path $repoPath 'scripts/prepare-template-composition-upgrade.ps1'
$outputPath = Join-Path $testPath 'output'
& $helper -TemplatesPath $inputPath -OutputDirectory $outputPath -DatabaseName rehearsal
$convertedFile = Join-Path $outputPath 'templates/page.json'
$converted = Get-Content -LiteralPath $convertedFile -Raw | ConvertFrom-Json
if ($converted.id -ne $pageId) { throw 'Page identity changed.' }
if (($converted.baseTemplates -join ',') -ne 'standard') { throw 'Page did not inherit Standard.' }
if ($converted.templateId -ne 'a45f8728-e65f-459c-8a64-099c74d9f08e') { throw 'Defining template missing.' }
if (@($converted.sections.fields | Where-Object key -eq 'jsonLd').Count -ne 1) { throw 'SEO was lost.' }
if (@($converted.sections.fields | Where-Object key -eq 'body').Count -ne 1) { throw 'Body was lost or duplicated.' }
if ((Get-FileHash -LiteralPath $inputFile).Hash -ne $before) { throw 'Input file was modified.' }
if ((Get-FileHash -LiteralPath (Join-Path $outputPath 'original/page.json')).Hash -ne $before) { throw 'Original backup differs.' }
$sql = Get-Content -LiteralPath (Join-Path $outputPath 'reassign-template-items.sql') -Raw
if (-not $sql.Contains($pageId) -or -not $sql.Contains('DECLARE @Apply bit = 0')) { throw 'SQL target or rollback default is wrong.' }
if (-not $sql.Contains("('be9b2863-eb2d-4d2e-8990-884a87ab6a0b', '$titleId')")) { throw 'Title value identity remapping missing.' }
$secondOutput = Join-Path $testPath 'second-output'
& $helper -TemplatesPath (Join-Path $outputPath 'templates') -OutputDirectory $secondOutput -DatabaseName rehearsal
$second = Get-Content -LiteralPath (Join-Path $secondOutput 'templates/page.json') -Raw | ConvertFrom-Json
if (@($second.sections.fields | Where-Object key -eq 'body').Count -ne 1) { throw 'Repeated conversion duplicated Body.' }
$emptyInput = Join-Path $testPath 'empty-input'
[void](New-Item -ItemType Directory -Path $emptyInput)
$emptyOutput = Join-Path $testPath 'empty-output'
& $helper -TemplatesPath $emptyInput -OutputDirectory $emptyOutput -DatabaseName rehearsal
$starter = Get-Content -LiteralPath (Join-Path $emptyOutput 'templates/page.json') -Raw | ConvertFrom-Json
if ($starter.id -ne '562ba716-a878-45e5-9ba7-397f46ba7b1d') { throw 'Starter Page changed the legacy identity.' }
foreach ($scenario in @('additional-base', 'incompatible-scope')) {
    $fixture.baseTemplates = if ($scenario -eq 'additional-base') { @('item','customBase') } else { @('item') }
    $fixture.sections[0].fields[1].isUnversioned = $scenario -ne 'incompatible-scope'
    $fixture | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $inputFile
    $refused = $false
    try { & $helper -TemplatesPath $inputPath -OutputDirectory (Join-Path $testPath $scenario) -DatabaseName rehearsal }
    catch { $refused = $true }
    if (-not $refused) { throw "Unsafe scenario accepted: $scenario" }
}
Write-Output 'PASS: source preservation, Page identity, SEO preservation, defining template, inheritance, Body, repeat conversion, rollback SQL, and empty-instance conversion.'
