CREATE PROCEDURE dbo.Sync_BulkDelete (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME VARCHAR(100)
        DECLARE @CREATION_FIELD VARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1
        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME +'_bulkdelete]
        @sync_min_timestamp BigInt,
        @sync_scope_local_id Int,
        @changeTable [' + @ATABLENAME + '_BulkType] READONLY AS
        BEGIN
            declare @changed TABLE ([' + @KEY_NAME + '] int, PRIMARY KEY ([' + @KEY_NAME + ']));
            DELETE [' + @ATABLENAME + '] OUTPUT DELETED.[' + @KEY_NAME + '] INTO @changed FROM [' + @ATABLENAME + '] base JOIN
            (SELECT	p.*,
                    t.update_scope_local_id,
                    t.scope_update_peer_key,
                    t.local_update_peer_timestamp
            FROM @changeTable p	JOIN
            [' + @ATABLENAME + '_tracking] t ON p.[' + @KEY_NAME + '] = t.[' + @KEY_NAME + ']) as changes ON changes.[' + @KEY_NAME + '] = base.[' + @KEY_NAME + ']
            WHERE (changes.update_scope_local_id = @sync_scope_local_id AND changes.scope_update_peer_key = changes.sync_update_peer_key) OR
            changes.local_update_peer_timestamp <= @sync_min_timestamp
            UPDATE side SET
                sync_row_is_tombstone = 1,
                update_scope_local_id = @sync_scope_local_id,
                scope_update_peer_key = changes.sync_update_peer_key,
                scope_update_peer_timestamp = changes.sync_update_peer_timestamp,
                local_update_peer_key = 0
            FROM [' + @ATABLENAME + '_tracking] side JOIN
                (SELECT	p.[' + @KEY_NAME + '],
                        p.sync_update_peer_timestamp,
                        p.sync_update_peer_key,
                        p.sync_create_peer_key,
                        p.sync_create_peer_timestamp
                FROM @changed t JOIN
                @changeTable p ON p.[' + @KEY_NAME + '] = t.[' + @KEY_NAME + '])
            AS changes ON changes.[' + @KEY_NAME + '] = side.[' + @KEY_NAME + ']
            SELECT [' + @KEY_NAME + '] FROM @changeTable t WHERE NOT EXISTS (SELECT [' + @KEY_NAME + '] from @changed i WHERE t.[' + @KEY_NAME + '] = i.[' + @KEY_NAME + '])
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
        END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END