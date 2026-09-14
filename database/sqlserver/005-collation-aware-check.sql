-- Collation-aware check examples
-- Run in templarcms_authoring and templarcms_published

-- Check server vs database collation
SELECT SERVERPROPERTY('Collation') AS ServerCollation,
       DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DatabaseCollation;

-- Confirm case-insensitive and accent-sensitive behavior
SELECT
  CASE WHEN 'Home' = 'home' COLLATE SQL_Latin1_General_CP1_CI_AS THEN 1 ELSE 0 END AS CaseInsensitive,
  CASE WHEN 'resume' = 'résumé' COLLATE SQL_Latin1_General_CP1_CI_AS THEN 1 ELSE 0 END AS AccentEqual;
