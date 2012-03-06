CREATE TABLE [dbo].[Orders] (
    [ID]         INT        IDENTITY NOT NULL,
    [CustomerID] INT        NOT NULL,
    [Item]       NVARCHAR(50) NOT NULL,
    [Cost]       MONEY      NOT NULL,
    [Date]       DATETIME   NOT NULL
);