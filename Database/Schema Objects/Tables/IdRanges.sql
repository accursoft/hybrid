CREATE TABLE [dbo].[IdRanges]
(
    [Id] INT NOT NULL PRIMARY KEY, 
    [Machine] NCHAR(25) NOT NULL, 
    [Min] INT NOT NULL, 
    [Max] INT NOT NULL
)
