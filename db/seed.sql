-- Fake local data only. The API keys are placeholders, they do not work against Binance.
IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = 'SUPERADMIN')
    INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp) VALUES (CONVERT(nvarchar(450), NEWID()), 'superadmin', 'SUPERADMIN', CONVERT(nvarchar(450), NEWID()));
IF NOT EXISTS (SELECT 1 FROM dbo.AuthKeys WHERE [Key] = 'LOCAL-DEV-KEY')
    INSERT INTO dbo.AuthKeys ([Key], CurrencyPool, CurrencyType) VALUES ('LOCAL-DEV-KEY', 1000, 'USDT');
IF NOT EXISTS (SELECT 1 FROM dbo.TraderGroups)
BEGIN
    INSERT INTO dbo.TraderGroups (Name) VALUES ('Local test group');
    DECLARE @groupId int = SCOPE_IDENTITY();
    DECLARE @traderId int, @copy1 int, @copy2 int;

    INSERT INTO dbo.UserAPIs (UserId, APIKey, Secure, IsActive, TraderGroupId, FullName, BinanceUserId)
    VALUES ('seed-trader', 'LOCAL_TEST_TRADER_API_KEY_0000', 'LOCAL_TEST_TRADER_SECRET_00000', 1, NULL, 'TEST Trader One', '1001');
    SET @traderId = SCOPE_IDENTITY();
    INSERT INTO dbo.Traders (TraderGroupId, UserAPIId) VALUES (@groupId, @traderId);

    INSERT INTO dbo.UserAPIs (UserId, APIKey, Secure, IsActive, TraderGroupId, FullName, BinanceUserId)
    VALUES ('seed-follower-1', 'LOCAL_TEST_COPY1_API_KEY_0000', 'LOCAL_TEST_COPY1_SECRET_00000', 1, @groupId, 'TEST Follower One', '2001');
    SET @copy1 = SCOPE_IDENTITY();
    INSERT INTO dbo.UserAPIs (UserId, APIKey, Secure, IsActive, TraderGroupId, FullName, BinanceUserId)
    VALUES ('seed-follower-2', 'LOCAL_TEST_COPY2_API_KEY_0000', 'LOCAL_TEST_COPY2_SECRET_00000', 1, @groupId, 'TEST Follower Two', '2002');
    SET @copy2 = SCOPE_IDENTITY();

    INSERT INTO dbo.Balances (UserAPIId, CurrencyAmount, CurrentAmount, CurrencyType) VALUES
        (@traderId, 10000, 10000, 'USDT'), (@copy1, 1000, 1000, 'USDT'), (@copy2, 500, 500, 'USDT');
END
GO
