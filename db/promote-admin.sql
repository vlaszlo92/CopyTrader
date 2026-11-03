-- Usage: sqlcmd ... -v ADMIN_EMAIL=you@example.com -i db/promote-admin.sql
-- Register the user in the web UI first (registration key: LOCAL-DEV-KEY).
INSERT INTO dbo.AspNetUserRoles (UserId, RoleId)
SELECT u.Id, r.Id
FROM dbo.AspNetUsers u CROSS JOIN dbo.AspNetRoles r
WHERE u.NormalizedEmail = UPPER('$(ADMIN_EMAIL)') AND r.NormalizedName = 'SUPERADMIN'
  AND NOT EXISTS (SELECT 1 FROM dbo.AspNetUserRoles x WHERE x.UserId = u.Id AND x.RoleId = r.Id);
PRINT CONCAT('Rows promoted: ', @@ROWCOUNT);
GO
