-- Over-length values audit script
-- Run in templarcms_authoring and templarcms_published

-- ContentItems.Name > 255
SELECT 'dbo.ContentItems' AS TableName, Id, [Name] AS Value, DATALENGTH([Name]) / 2 AS CharCount, 255 AS Limit
FROM dbo.ContentItems
WHERE DATALENGTH([Name]) / 2 > 255;

-- ContentItems.Icon > 255
SELECT 'dbo.ContentItems' AS TableName, Id, [Icon] AS Value, DATALENGTH([Icon]) / 2 AS CharCount, 255 AS Limit
FROM dbo.ContentItems
WHERE [Icon] IS NOT NULL AND DATALENGTH([Icon]) / 2 > 255;

-- ContentFieldValues.FieldKey > 450
SELECT 'dbo.ContentFieldValues' AS TableName, Id, [FieldKey] AS Value, DATALENGTH([FieldKey]) / 2 AS CharCount, 450 AS Limit
FROM dbo.ContentFieldValues
WHERE DATALENGTH([FieldKey]) / 2 > 450;

-- MediaAssets.FileName > 255
SELECT 'dbo.MediaAssets' AS TableName, Id, [FileName] AS Value, DATALENGTH([FileName]) / 2 AS CharCount, 255 AS Limit
FROM dbo.MediaAssets
WHERE DATALENGTH([FileName]) / 2 > 255;

-- MediaAssets.StoredFileName > 500
SELECT 'dbo.MediaAssets' AS TableName, Id, [StoredFileName] AS Value, DATALENGTH([StoredFileName]) / 2 AS CharCount, 500 AS Limit
FROM dbo.MediaAssets
WHERE DATALENGTH([StoredFileName]) / 2 > 500;

-- MediaAssets.ContentType > 255
SELECT 'dbo.MediaAssets' AS TableName, Id, [ContentType] AS Value, DATALENGTH([ContentType]) / 2 AS CharCount, 255 AS Limit
FROM dbo.MediaAssets
WHERE DATALENGTH([ContentType]) / 2 > 255;

-- MediaAssets.AltText > 500
SELECT 'dbo.MediaAssets' AS TableName, Id, [AltText] AS Value, DATALENGTH([AltText]) / 2 AS CharCount, 500 AS Limit
FROM dbo.MediaAssets
WHERE [AltText] IS NOT NULL AND DATALENGTH([AltText]) / 2 > 500;

-- MediaAssets.Title > 255
SELECT 'dbo.MediaAssets' AS TableName, Id, [Title] AS Value, DATALENGTH([Title]) / 2 AS CharCount, 255 AS Limit
FROM dbo.MediaAssets
WHERE [Title] IS NOT NULL AND DATALENGTH([Title]) / 2 > 255;