CREATE PROCEDURE dbo.Sync_Delete (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_delete]
        @P_1 Int,
        @sync_force_write Int,
        @sync_min_timestamp BigInt,
        @sync_row_count Int OUTPUT AS
        BEGIN
            SET @sync_row_count = 0;
            DELETE [' + @ATABLENAME + '] FROM
            [' + @ATABLENAME + '] [base] JOIN [' + @ATABLENAME + '_tracking] [side] ON [base].[' + @KEY_NAME + '] = [side].[' + @KEY_NAME + ']
            WHERE
            ([side].[local_update_peer_timestamp] <= @sync_min_timestamp OR @sync_force_write = 1) AND ([base].[' + @KEY_NAME + '] = @P_1);
            SET @sync_row_count = @@ROWCOUNT;
        END'

        --PRINT @CMD
        EXEC SP_EXECUTESQL @CMD
    END
    ELSE
        PRINT 'ERROR : UNABLE TO FOUND TEMPORARY TABLE OR TABLE NAME INVALID'
END