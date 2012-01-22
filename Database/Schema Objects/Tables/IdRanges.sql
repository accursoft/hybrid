CREATE TABLE [dbo].[IdRanges]
(
    [Id] INT NOT NULL PRIMARY KEY IDENTITY, 
    [Machine] NCHAR(25) NOT NULL, 
    [Min] INT NOT NULL, 
    [Max] INT NOT NULL
)
