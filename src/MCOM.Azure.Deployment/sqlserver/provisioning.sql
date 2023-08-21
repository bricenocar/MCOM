CREATE USER [logig-mcom-provisioning-inttest] FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[dbo]
GO

ALTER ROLE [db_datawriter] ADD MEMBER [logig-mcom-provisioning-inttest]
GO