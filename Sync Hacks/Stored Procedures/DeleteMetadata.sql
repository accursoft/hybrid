CREATE PROCEDURE dbo.Sync_DeleteMetadata (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_deletemetadata]
        @P_1 Int,
        @sync_check_concurrency Int,
        @sync_row_timestamp BigInt,
        @sync_row_count Int OUTPUT AS
        BEGIN
            SET @sync_row_count = 0;
            DELETE [side] FROM [' + @ATABLENAME + '_tracking] [side] WHERE
            [' + @KEY_NAME + '] = @P_1 AND (@sync_check_concurrency = 0 or [local_update_peer_timestamp] = @sync_row_timestamp);
            SET @sync_row_count = @@ROWCOUNT;
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
    END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END