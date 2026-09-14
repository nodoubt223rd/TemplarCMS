-- Duplicate checks that block unique-index changes
-- Run in templarcms_authoring and templarcms_published

-- DirectoryUsers.NormalizedEmail duplicates
SELECT NormalizedEmail, COUNT(*) AS Cnt
FROM dbo.DirectoryUsers
GROUP BY NormalizedEmail
HAVING COUNT(*) > 1;

-- ContentItems root keys duplicates (ParentId IS NULL)
SELECT [Key], COUNT(*) AS Cnt
FROM dbo.ContentItems
WHERE ParentId IS NULL
GROUP BY [Key]
HAVING COUNT(*) > 1;

-- ContentItems sibling key duplicates (same parent)
SELECT ParentId, [Key], COUNT(*) AS Cnt
FROM dbo.ContentItems
WHERE ParentId IS NOT NULL
GROUP BY ParentId, [Key]
HAVING COUNT(*) > 1;

-- ContentFieldValues unique constraint potential duplicates:
-- duplicate (ItemId, FieldId, Language, Version)
SELECT ItemId, FieldId, Language, Version, COUNT(*) AS Cnt
FROM dbo.ContentFieldValues
GROUP BY ItemId, FieldId, Language, Version
HAVING COUNT(*) > 1;