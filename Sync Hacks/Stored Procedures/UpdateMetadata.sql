CREATE PROCEDURE dbo.Sync_UpdateMetadata (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_updatemetadata]
        @P_1 Int,
        @sync_scope_local_id Int,
        @sync_row_is_tombstone Int,
        @sync_create_peer_key Int,
        @sync_create_peer_timestamp BigInt,
        @sync_update_peer_key Int,
        @sync_update_peer_timestamp BigInt,
        @sync_check_concurrency Int,
        @sync_row_timestamp BigInt,
        @sync_row_count Int OUTPUT AS
        BEGIN
            SET @sync_row_count = 0;
            DECLARE @was_tombstone int;

            SELECT @was_tombstone = [sync_row_is_tombstone]
            FROM [' + @ATABLENAME + '_tracking]
            WHERE ([' + @KEY_NAME + '] = @P_1);

            IF (@was_tombstone IS NOT NULL AND @was_tombstone = 1 AND @sync_row_is_tombstone = 0)
                UPDATE [' + @ATABLENAME + '_tracking] SET
                    [create_scope_local_id] = @sync_scope_local_id,
                    [scope_create_peer_key] = @sync_create_peer_key,
                    [scope_create_peer_timestamp] = @sync_create_peer_timestamp,
                    [local_create_peer_key] = 0,
                    [local_create_peer_timestamp] = @@DBTS+1,
                    [update_scope_local_id] = @sync_scope_local_id,
                    [scope_update_peer_key] = @sync_update_peer_key,
                    [scope_update_peer_timestamp] = @sync_update_peer_timestamp,
                    [local_update_peer_key] = 0,
                    [restore_timestamp] = NULL,
                    [sync_row_is_tombstone] = @sync_row_is_tombstone
                WHERE ([' + @KEY_NAME + '] = @P_1) AND (@sync_check_concurrency = 0 or [local_update_peer_timestamp] = @sync_row_timestamp);
            ELSE
                UPDATE [' + @ATABLENAME + '_tracking] SET
                    [update_scope_local_id] = @sync_scope_local_id,
                    [scope_update_peer_key] = @sync_update_peer_key,
                    [scope_update_peer_timestamp] = @sync_update_peer_timestamp,
                    [local_update_peer_key] = 0,
                    [restore_timestamp] = NULL,
                    [sync_row_is_tombstone] = @sync_row_is_tombstone
                WHERE ([' + @KEY_NAME + '] = @P_1) AND (@sync_check_concurrency = 0 or [local_update_peer_timestamp] = @sync_row_timestamp);

            SET @sync_row_count = @@ROWCOUNT;
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
    END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END