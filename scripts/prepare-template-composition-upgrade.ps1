<#
.SYNOPSIS
Prepares an offline copy of legacy template JSON and SQL data reassignment.
.DESCRIPTION
Does not modify the input directory or connect to a database. Use a fresh output
directory, review its diff and SQL, and deploy with the application stopped.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$TemplatesPath,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9_]+$')][string]$DatabaseName
)
$ErrorActionPreference = 'Stop'
$inputPath = (Resolve-Path -LiteralPath $TemplatesPath).Path
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputPath) { throw 'OutputDirectory must not already exist.' }
if ($outputPath.StartsWith($inputPath.TrimEnd('\') + '\', [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be outside the input templates directory.'
}
$legacyId = '562ba716-a878-45e5-9ba7-397f46ba7b1d'
$metaId = 'a45f8728-e65f-459c-8a64-099c74d9f08e'
$reserved = @('standard','template','folder','advanced','appearance','help','lifetime','publishing','statistics','tasks','version','item')
$documents = @(Get-ChildItem -LiteralPath $inputPath -Filter '*.json' -File | ForEach-Object {
    $document = Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json -AsHashtable
    if (-not $document.key -or -not $document.id) { throw "Missing template identity in $($_.Name)." }
    if ($reserved -contains $document.key) { throw "Authored template '$($document.key)' conflicts with a system or retired key. Resolve it before migration." }
    [void][guid]::Parse($document.id)
    @{ File = $_; Document = $document }
})
$pages = @($documents | Where-Object { $_.Document.key -eq 'page' })
if ($pages.Count -gt 1) { throw 'Multiple Page definitions exist.' }
$page = if ($pages.Count -eq 1) { $pages[0].Document } else {
    @{ id=$legacyId; name='Page'; key='page'; icon='file'; baseTemplates=@('standard'); sections=@() }
}
$pageId = ([guid]$page.id).ToString()
if (@($documents | Where-Object { $_.Document.id -eq $legacyId -and $_.Document.key -ne 'page' }).Count) {
    throw 'The legacy Item ID is already assigned to another authored template.'
}
$body = @{ id='ea54795d-4fbe-477b-a2cc-f8da57485729'; name='Body'; key='body'; fieldType='RichText'; isShared=$false; isUnversioned=$false; metadata=@{} }
foreach ($entry in $documents) {
    $doc = $entry.Document
    $legacyBase = @($doc.baseTemplates) -contains 'item'
    if (($legacyBase -or $doc.key -eq 'page') -and @($doc.baseTemplates | Where-Object { $_ -notin @('item','standard') }).Count) {
        throw "Template '$($doc.key)' has additional bases. Resolve its effective fields and prepare a manual conversion; automatic Body replacement could change inherited overrides."
    }
    if ($legacyBase) {
        $doc.baseTemplates = @($doc.baseTemplates | ForEach-Object { if ($_ -eq 'item') { 'standard' } else { $_ } } | Select-Object -Unique)
    }
    if ($legacyBase -or $doc.key -eq 'page') {
        if ($doc.key -eq 'page' -and @($doc.baseTemplates) -notcontains 'standard') {
            $doc.baseTemplates = @('standard') + @($doc.baseTemplates)
        }
        if (-not @($doc.sections | ForEach-Object { $_.fields } | Where-Object { $_.key -eq 'body' }).Count) {
            $content = @($doc.sections | Where-Object { $_.key -eq 'content' })
            if ($content.Count -gt 1) { throw "Duplicate Content sections on $($doc.key)." }
            if ($content.Count -eq 0) {
                $doc.sections = @($doc.sections) + @(@{ id='6a1ae5e6-bb4e-4eb9-90be-fa03c50d9c6d'; name='Content'; key='content'; sortOrder=100; fields=@($body) })
            } else { $content[0].fields = @($content[0].fields) + @($body) }
        }
    }
    $doc.templateId = $metaId
}
if ($pages.Count -eq 0) {
    $page.templateId = $metaId
    $page.sections = @(@{ id='6a1ae5e6-bb4e-4eb9-90be-fa03c50d9c6d'; name='Content'; key='content'; sortOrder=100; fields=@($body) })
}
$legacyFields = @(
    @{ key='title'; id='be9b2863-eb2d-4d2e-8990-884a87ab6a0b'; type='SingleLineText'; unversioned=$true },
    @{ key='navigationTitle'; id='d315d9af-f921-4385-bd24-8a97bce1afa3'; type='SingleLineText'; unversioned=$true },
    @{ key='metaDescription'; id='b6a8a944-f09a-4779-83eb-1abea205f51c'; type='MultiLineText'; unversioned=$true },
    @{ key='body'; id='ea54795d-4fbe-477b-a2cc-f8da57485729'; type='RichText'; unversioned=$false }
)
$pageLocalFields = @($page.sections | ForEach-Object { $_.fields })
if (@($pageLocalFields | Where-Object { $_.key -like '__*' }).Count) {
    throw 'Page overrides system fields. Review field identities and scopes manually before reassigning legacy items.'
}
$fieldMappings = foreach ($legacyField in $legacyFields) {
    $overrides = @($pageLocalFields | Where-Object { $_.key -eq $legacyField.key })
    if ($overrides.Count -gt 1) { throw "Duplicate Page field $($legacyField.key)." }
    $targetId = $legacyField.id
    if ($overrides.Count -eq 1) {
        $field = $overrides[0]
        if ($field.fieldType -ne $legacyField.type -or [bool]$field.isShared -or [bool]$field.isUnversioned -ne $legacyField.unversioned) {
            throw "Page field '$($legacyField.key)' changes type or scope. Review a value conversion before reassigning legacy items."
        }
        $targetId = ([guid]$field.id).ToString()
    }
    "('$($legacyField.id)', '$targetId')"
}
$mappingSql = $fieldMappings -join ",`n"
$backupPath = Join-Path $outputPath 'original'
$convertedPath = Join-Path $outputPath 'templates'
[void](New-Item -ItemType Directory -Path $backupPath, $convertedPath)
foreach ($entry in $documents) {
    Copy-Item -LiteralPath $entry.File.FullName -Destination $backupPath
    $entry.Document | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $convertedPath $entry.File.Name) -Encoding utf8
}
if ($pages.Count -eq 0) { $page | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath (Join-Path $convertedPath 'page.json') -Encoding utf8 }
@"
-- Review with a current backup and the matching converted JSON. Run during a write pause.
USE [$DatabaseName];
SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Apply bit = 0; -- Set to 1 only after reviewing the preview below.
SELECT Id, Name, TemplateId AS PreviousTemplateId, '$pageId' AS NewTemplateId
FROM dbo.ContentItems WHERE TemplateId = '$legacyId';
DECLARE @FieldMap TABLE (OldId uniqueidentifier PRIMARY KEY, NewId uniqueidentifier NOT NULL);
INSERT INTO @FieldMap (OldId, NewId) VALUES
$mappingSql;
-- Keep the stored values attached when Page redeclares a field with a different ID.
-- Type and scope compatibility were checked when this script was prepared.
IF EXISTS (
    SELECT 1 FROM dbo.ContentFieldValues source
    JOIN dbo.ContentItems item ON item.Id = source.ItemId AND item.TemplateId = '$legacyId'
    JOIN @FieldMap mapping ON mapping.OldId = source.FieldId AND mapping.OldId <> mapping.NewId
    JOIN dbo.ContentFieldValues target ON target.ItemId = source.ItemId
        AND target.FieldId = mapping.NewId AND target.Language = source.Language AND target.Version = source.Version
)
    THROW 51000, 'Target field values already exist. Review conflicts before conversion.', 1;
SELECT source.ItemId, source.FieldKey, source.FieldId AS PreviousFieldId, mapping.NewId AS NewFieldId
FROM dbo.ContentFieldValues source
JOIN dbo.ContentItems item ON item.Id = source.ItemId AND item.TemplateId = '$legacyId'
JOIN @FieldMap mapping ON mapping.OldId = source.FieldId AND mapping.OldId <> mapping.NewId;
UPDATE source SET FieldId = mapping.NewId
FROM dbo.ContentFieldValues source
JOIN dbo.ContentItems item ON item.Id = source.ItemId AND item.TemplateId = '$legacyId'
JOIN @FieldMap mapping ON mapping.OldId = source.FieldId AND mapping.OldId <> mapping.NewId;
UPDATE dbo.ContentItems SET TemplateId = '$pageId'
WHERE TemplateId = '$legacyId';
UPDATE dbo.ContentItems SET TemplateId = '$metaId'
WHERE Id = '74922877-d8df-466b-93ce-96e5c91d5b7e'
  AND TemplateId = '6991d76d-6475-4a2b-b04f-d16e9e4aae9f';
IF @Apply = 1 COMMIT TRANSACTION; ELSE ROLLBACK TRANSACTION;
"@ | Set-Content -LiteralPath (Join-Path $outputPath 'reassign-template-items.sql') -Encoding utf8
Write-Output "Prepared $convertedPath. Originals copied to $backupPath. SQL defaults to rollback. No live files or databases were changed."
