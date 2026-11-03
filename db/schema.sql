-- Local development schema, reconstructed from the EF Core model (src/web/Areas/Identity/Data)
-- and the SQL used by the trader service. The EF migrations in the repository are outdated.
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.AspNetRoles') IS NULL
CREATE TABLE dbo.AspNetRoles (
    Id nvarchar(450) NOT NULL PRIMARY KEY,
    Name nvarchar(256) NULL,
    NormalizedName nvarchar(256) NULL,
    ConcurrencyStamp nvarchar(max) NULL
);
GO
IF OBJECT_ID('dbo.AspNetUsers') IS NULL
CREATE TABLE dbo.AspNetUsers (
    Id nvarchar(450) NOT NULL PRIMARY KEY,
    UserName nvarchar(255) NOT NULL,
    NormalizedUserName nvarchar(256) NULL,
    Email nvarchar(256) NULL,
    NormalizedEmail nvarchar(256) NULL,
    EmailConfirmed bit NOT NULL,
    PasswordHash nvarchar(max) NULL,
    SecurityStamp nvarchar(max) NULL,
    ConcurrencyStamp nvarchar(max) NULL,
    PhoneNumber nvarchar(max) NULL,
    PhoneNumberConfirmed bit NOT NULL,
    TwoFactorEnabled bit NOT NULL,
    LockoutEnd datetimeoffset NULL,
    LockoutEnabled bit NOT NULL,
    AccessFailedCount int NOT NULL,
    IsTrader bit NOT NULL DEFAULT 0,
    IsActive bit NOT NULL DEFAULT 1,
    RegisteredDate datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CurrencyAmount float NOT NULL DEFAULT 0,
    CurrencyType nvarchar(max) NULL,
    FirstName nvarchar(max) NULL,
    LastName nvarchar(max) NULL
);
GO
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
GO
CREATE TABLE dbo.AspNetUserRoles (
    UserId nvarchar(450) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    RoleId nvarchar(450) NOT NULL REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    PRIMARY KEY (UserId, RoleId)
);
CREATE TABLE dbo.AspNetUserClaims (
    Id int IDENTITY PRIMARY KEY,
    UserId nvarchar(450) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    ClaimType nvarchar(max) NULL,
    ClaimValue nvarchar(max) NULL
);
CREATE TABLE dbo.AspNetRoleClaims (
    Id int IDENTITY PRIMARY KEY,
    RoleId nvarchar(450) NOT NULL REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
    ClaimType nvarchar(max) NULL,
    ClaimValue nvarchar(max) NULL
);
CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider nvarchar(128) NOT NULL,
    ProviderKey nvarchar(128) NOT NULL,
    ProviderDisplayName nvarchar(max) NULL,
    UserId nvarchar(450) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    PRIMARY KEY (LoginProvider, ProviderKey)
);
CREATE TABLE dbo.AspNetUserTokens (
    UserId nvarchar(450) NOT NULL REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE,
    LoginProvider nvarchar(128) NOT NULL,
    Name nvarchar(128) NOT NULL,
    Value nvarchar(max) NULL,
    PRIMARY KEY (UserId, LoginProvider, Name)
);
GO
CREATE TABLE dbo.AuthKeys (
    Id int IDENTITY PRIMARY KEY,
    [Key] nvarchar(100) NOT NULL,
    CurrencyPool float NOT NULL DEFAULT 0,
    CurrencyType nvarchar(max) NULL
);
CREATE TABLE dbo.TraderGroups (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(max) NULL
);
CREATE TABLE dbo.UserAPIs (
    Id int IDENTITY PRIMARY KEY,
    UserId nvarchar(450) NULL,
    APIKey nvarchar(450) NULL,
    Secure nvarchar(450) NULL,
    IsActive bit NOT NULL DEFAULT 1,
    TraderGroupId int NULL,
    FullName nvarchar(max) NULL,
    BinanceUserId nvarchar(max) NOT NULL DEFAULT ''
);
CREATE TABLE dbo.Traders (
    Id int IDENTITY PRIMARY KEY,
    TraderGroupId int NOT NULL,
    UserAPIId int NOT NULL
);
CREATE TABLE dbo.Balances (
    Id int IDENTITY PRIMARY KEY,
    UserAPIId int NOT NULL,
    CurrencyAmount float NOT NULL DEFAULT 0,
    CurrentAmount float NOT NULL DEFAULT 0,
    CurrencyType nvarchar(max) NULL
);
CREATE TABLE dbo.Transactions (
    Id int IDENTITY PRIMARY KEY,
    user_api_id int NOT NULL,
    order_id_trader bigint NULL,
    symbol nvarchar(50) NOT NULL,
    client_order_id nvarchar(100) NULL,
    order_id bigint NOT NULL,
    side nvarchar(20) NULL,
    order_type nvarchar(50) NULL,
    quantity decimal(38,10) NULL,
    average_price decimal(38,10) NULL,
    order_status nvarchar(50) NULL,
    commission_asset decimal(38,10) NULL,
    working_type nvarchar(50) NULL,
    original_order_type nvarchar(50) NULL,
    position_side nvarchar(20) NULL,
    realized_profit decimal(38,10) NULL,
    json nvarchar(max) NULL,
    updated_at datetime2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO
CREATE OR ALTER PROCEDURE dbo.transaction_insert_update
    @user_api_id int, @order_id_trader bigint, @symbol nvarchar(50), @client_order_id nvarchar(100),
    @order_id bigint, @side nvarchar(20), @order_type nvarchar(50), @quantity decimal(38,10),
    @average_price decimal(38,10), @order_status nvarchar(50), @commission_asset decimal(38,10),
    @working_type nvarchar(50), @original_order_type nvarchar(50), @position_side nvarchar(20),
    @realized_profit decimal(38,10), @json nvarchar(max)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.Transactions
       SET order_id_trader = @order_id_trader, side = @side, order_type = @order_type, quantity = @quantity,
           average_price = @average_price, order_status = @order_status, commission_asset = @commission_asset,
           working_type = @working_type, original_order_type = @original_order_type, position_side = @position_side,
           realized_profit = @realized_profit, json = @json, updated_at = SYSUTCDATETIME()
     WHERE user_api_id = @user_api_id AND order_id = @order_id;
    IF @@ROWCOUNT = 0
        INSERT INTO dbo.Transactions (user_api_id, order_id_trader, symbol, client_order_id, order_id, side, order_type,
            quantity, average_price, order_status, commission_asset, working_type, original_order_type, position_side,
            realized_profit, json)
        VALUES (@user_api_id, @order_id_trader, @symbol, @client_order_id, @order_id, @side, @order_type,
            @quantity, @average_price, @order_status, @commission_asset, @working_type, @original_order_type,
            @position_side, @realized_profit, @json);
END
GO
