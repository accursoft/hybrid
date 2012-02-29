CREATE PROCEDURE dbo.Sync_BulkUpdate (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @FIELD_NAME NVARCHAR(50)
        -- DECLARE @FIELDS NVARCHAR(1000)
        DECLARE @CHANGES_FIELDS NVARCHAR(1000)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1
        -- SET @FIELDS = ''
        SET @CHANGES_FIELDS = ''

        DECLARE FIELD_LIST CURSOR FAST_FORWARD FOR SELECT COLUMN_NAME FROM ##TABLE_DESCRIPTION
        OPEN FIELD_LIST
        FETCH NEXT FROM FIELD_LIST INTO @FIELD_NAME
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @CHANGES_FIELDS <> ''
                SET @CHANGES_FIELDS = @CHANGES_FIELDS + ', '
            IF @FIELD_NAME <> @KEY_NAME
                SET @CHANGES_FIELDS = @CHANGES_FIELDS + '[' + @FIELD_NAME + '] = changes.[' + @FIELD_NAME + ']'
            FETCH NEXT FROM FIELD_LIST INTO @FIELD_NAME
        END
        CLOSE FIELD_LIST
        DEALLOCATE FIELD_LIST

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_bulkupdate]
        @sync_min_timestamp BigInt,
        @sync_scope_local_id Int,
        @changeTable [' + @ATABLENAME + '_BulkType] READONLY AS
        BEGIN
            -- use a temp table to store the list of PKs that successfully got updated
            declare @changed TABLE ([' + @KEY_NAME + '] int, PRIMARY KEY ([' + @KEY_NAME + ']));
            SET IDENTITY_INSERT ['
            + @ATABLENAME + '] ON;
            -- update the base table
            MERGE [' + @ATABLENAME + '] AS base USING
            -- join done here against the side table to get the local timestamp for concurrency check
                (SELECT p.*,
                        t.update_scope_local_id,
                        t.scope_update_peer_key,
                        t.local_update_peer_timestamp
                FROM @changeTable p	LEFT JOIN
                [' + @ATABLENAME + '_tracking] t ON p.[' + @KEY_NAME + '] = t.[' + @KEY_NAME + '])
            AS changes ON changes.[' + @KEY_NAME + '] = base.[' + @KEY_NAME + ']
            WHEN MATCHED AND (changes.update_scope_local_id = @sync_scope_local_id AND changes.scope_update_peer_key = changes.sync_update_peer_key) OR changes.local_update_peer_timestamp <= @sync_min_timestamp THEN
            UPDATE SET ' + @CHANGES_FIELDS + ' OUTPUT INSERTED.[' + @KEY_NAME + '] into @changed; -- populates the temp table with successful PKs
            SET IDENTITY_INSERT [' + @ATABLENAME + '] OFF;
            UPDATE side SET
                update_scope_local_id = @sync_scope_local_id,
                scope_update_peer_key = changes.sync_update_peer_key,
                scope_update_peer_timestamp = changes.sync_update_peer_timestamp,
                local_update_peer_key = 0
            FROM [' + @ATABLENAME + '_tracking] side JOIN
                (SELECT p.[' + @KEY_NAME + '],
                        p.sync_update_peer_timestamp,
                        p.sync_update_peer_key,
                        p.sync_create_peer_key,
                        p.sync_create_peer_timestamp
                FROM @changed t JOIN @changeTable p ON p.['	+ @KEY_NAME + '] = t.[' + @KEY_NAME + '])
            AS changes ON changes.[' + @KEY_NAME + '] = side.[' + @KEY_NAME + ']
            SELECT [' + @KEY_NAME + '] FROM	@changeTable t
            WHERE NOT EXISTS (SELECT ['	+ @KEY_NAME + '] from @changed i WHERE t.[' + @KEY_NAME + '] = i.[' + @KEY_NAME + '])
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
    END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END