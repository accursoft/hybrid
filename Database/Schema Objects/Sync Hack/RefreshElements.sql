CREATE PROCEDURE dbo.Sync_RefreshElements (@ATABLE_NAME NVARCHAR(100)) AS
BEGIN
    DECLARE @TABLE_NAME VARCHAR(100)
    DECLARE TRACK_TABLE_LIST CURSOR FAST_FORWARD FOR

    SELECT TBL.NAME FROM SYS.OBJECTS TBL INNER JOIN SYS.OBJECTS TRACK ON TRACK.NAME = TBL.NAME + '_TRACKING' AND TRACK.TYPE_DESC = 'USER_TABLE' AND TRACK.IS_MS_SHIPPED = 0
    WHERE TBL.TYPE_DESC = 'USER_TABLE' AND TBL.IS_MS_SHIPPED = 0 AND ((TBL.NAME = @ATABLE_NAME) OR (@ATABLE_NAME = ''))
    
    OPEN TRACK_TABLE_LIST
    FETCH NEXT FROM TRACK_TABLE_LIST INTO @TABLE_NAME
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL
            DROP TABLE ##TABLE_DESCRIPTION

        SELECT
            CO.COLUMN_NAME,

            CASE WHEN CU.CONSTRAINT_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_KEY,

            CO.DATA_TYPE +
                CASE CO.DATA_TYPE
                    WHEN 'SQL_VARIANT' THEN ''
                    WHEN 'TEXT' THEN ''
                    WHEN 'DECIMAL' THEN '(' + CAST(CO.NUMERIC_PRECISION_RADIX AS VARCHAR) + ', ' + CAST(CO.NUMERIC_SCALE AS VARCHAR) + ')'
                ELSE
                    COALESCE('(' +
                        CASE
                            WHEN CO.CHARACTER_MAXIMUM_LENGTH = -1 THEN 'MAX'
                            ELSE CAST(CO.CHARACTER_MAXIMUM_LENGTH AS VARCHAR)
                        END
                    + ')', '')
                END AS TYPE_DEF,

            CASE
                WHEN CO.IS_NULLABLE = 'NO' THEN 'NOT '
                ELSE ''
            END
                + 'NULL ' +
            CASE
                WHEN CO.COLUMN_DEFAULT IS NOT NULL
                THEN 'DEFAULT '+ CO.COLUMN_DEFAULT
                ELSE ''
            END AS TYPE_NULLABLE

        INTO ##TABLE_DESCRIPTION
        
        FROM INFORMATION_SCHEMA.COLUMNS CO LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE CU ON
        CU.TABLE_NAME = CO.TABLE_NAME AND CU.COLUMN_NAME LIKE '%' + CO.COLUMN_NAME + '%'
        
        WHERE CO.TABLE_NAME = @TABLE_NAME
        ORDER BY CO.ORDINAL_POSITION

        DECLARE @CMD NVARCHAR(500)

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_bulkdelete]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_bulkdelete]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_bulkinsert]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_bulkinsert]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_bulkupdate]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_bulkupdate]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_delete]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_delete]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_deletemetadata]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_deletemetadata]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_insert]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_insert]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_insertmetadata]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_insertmetadata]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_selectchanges]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_selectchanges]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_selectrow]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_selectrow]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_update]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_update]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N''[dbo].[' + @TABLE_NAME + '_updatemetadata]'') AND type in (N''P'', N''PC''))
        DROP PROCEDURE [dbo].[' + @TABLE_NAME + '_updatemetadata]'
        EXEC SP_EXECUTESQL @CMD

        SET @CMD = 'IF EXISTS (SELECT * FROM sys.types st JOIN sys.schemas ss ON st.schema_id = ss.schema_id WHERE st.name = N''' + @TABLE_NAME + '_BulkType'' AND ss.name = N''dbo'')
        DROP TYPE [dbo].[' + @TABLE_NAME + '_BulkType]'
        EXEC SP_EXECUTESQL @CMD

        -- ALL SP & TYPES ARE DELETED, LET'S GO CREATE REFRESHED ELEMENTS
        EXEC dbo.Sync_TableType @TABLE_NAME
        EXEC dbo.Sync_BulkInsert @TABLE_NAME
        EXEC dbo.Sync_BulkDelete @TABLE_NAME
        EXEC dbo.Sync_BulkUpdate @TABLE_NAME
        EXEC dbo.Sync_Delete @TABLE_NAME
        EXEC dbo.Sync_DeleteMetadata @TABLE_NAME
        EXEC dbo.Sync_Insert @TABLE_NAME
        EXEC dbo.Sync_InsertMetadata @TABLE_NAME
        EXEC dbo.Sync_SelectChanges @TABLE_NAME
        EXEC dbo.Sync_SelectRow @TABLE_NAME
        EXEC dbo.Sync_Update @TABLE_NAME
        EXEC dbo.Sync_UpdateMetadata @TABLE_NAME

        FETCH NEXT FROM TRACK_TABLE_LIST INTO @TABLE_NAME
    END
    CLOSE TRACK_TABLE_LIST
    DEALLOCATE TRACK_TABLE_LIST
    DROP TABLE ##TABLE_DESCRIPTION
END