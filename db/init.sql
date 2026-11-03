IF DB_ID('CopyTrader') IS NULL CREATE DATABASE CopyTrader;
GO
IF SUSER_ID('copytrader_app') IS NULL CREATE LOGIN copytrader_app WITH PASSWORD = '$(APP_PASSWORD)', CHECK_POLICY = OFF;
GO
USE CopyTrader;
GO
IF USER_ID('copytrader_app') IS NULL CREATE USER copytrader_app FOR LOGIN copytrader_app;
GO
ALTER ROLE db_datareader ADD MEMBER copytrader_app;
ALTER ROLE db_datawriter ADD MEMBER copytrader_app;
GRANT EXECUTE TO copytrader_app;
GO
