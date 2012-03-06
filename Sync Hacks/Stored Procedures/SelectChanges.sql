CREATE PROCEDURE dbo.Sync_SelectChanges (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @FIELD_NAME NVARCHAR(50)
        DECLARE @UPDATE_FIELDS NVARCHAR(1000)
        DECLARE @COLUMN_NAME NVARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1
        SET @UPDATE_FIELDS = ''

        DECLARE FIELD_LIST CURSOR FAST_FORWARD FOR SELECT COLUMN_NAME FROM ##TABLE_DESCRIPTION
        OPEN FIELD_LIST
        FETCH NEXT FROM FIELD_LIST INTO @COLUMN_NAME
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF @COLUMN_NAME = @KEY_NAME
                SET @UPDATE_FIELDS = @UPDATE_FIELDS + '[side].[' + @COLUMN_NAME + '],'
            ELSE
                SET @UPDATE_FIELDS = @UPDATE_FIELDS + '[base].[' + @COLUMN_NAME + '],'

            FETCH NEXT FROM FIELD_LIST INTO @COLUMN_NAME
        END
        CLOSE FIELD_LIST
        DEALLOCATE FIELD_LIST

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_selectchanges]
        @sync_min_timestamp BigInt,
        @sync_scope_local_id Int,
        @sync_scope_restore_count Int,
        @sync_update_peer_key Int AS
        BEGIN
            SELECT '
                + @UPDATE_FIELDS + 
                '[side].[sync_row_is_tombstone],
                [side].[local_update_peer_timestamp] as sync_row_timestamp,

                case
                    when ([side].[update_scope_local_id] is null or [side].[update_scope_local_id] <> @sync_scope_local_id)
                    then COALESCE([side].[restore_timestamp], [side].[local_update_peer_timestamp])
                    else [side].[scope_update_peer_timestamp]
                end as sync_update_peer_timestamp,

                case
                    when ([side].[update_scope_local_id] is null or [side].[update_scope_local_id] <> @sync_scope_local_id)
                    then case
                            when ([side].[local_update_peer_key] > @sync_scope_restore_count)
                            then @sync_scope_restore_count
                            else [side].[local_update_peer_key]
                         end
                    else [side].[scope_update_peer_key]
                end as sync_update_peer_key,

                case
                    when ([side].[create_scope_local_id] is null or [side].[create_scope_local_id] <> @sync_scope_local_id)
                    then [side].[local_create_peer_timestamp]
                    else [side].[scope_create_peer_timestamp]
                end as sync_create_peer_timestamp,

                case
                    when ([side].[create_scope_local_id] is null or [side].[create_scope_local_id] <> @sync_scope_local_id)
                    then case
                             when ([side].[local_create_peer_key] > @sync_scope_restore_count)
                             then @sync_scope_restore_count else [side].[local_create_peer_key]
                          end
                    else [side].[scope_create_peer_key]
                end as sync_create_peer_key

            FROM [' + @ATABLENAME + '] [base] RIGHT JOIN [' + @ATABLENAME + '_tracking] [side] ON [base].[' + @KEY_NAME + '] = [side].[' + @KEY_NAME + ']
            WHERE
                    ([side].[update_scope_local_id] IS NULL
                    OR [side].[update_scope_local_id] <> @sync_scope_local_id
                    OR ([side].[update_scope_local_id] = @sync_scope_local_id
                    AND [side].[scope_update_peer_key] <> @sync_update_peer_key))
                AND [side].[local_update_peer_timestamp] > @sync_min_timestamp
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
    END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END