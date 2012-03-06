CREATE PROCEDURE dbo.Sync_Update (@ATABLENAME VARCHAR(100)) AS
BEGIN
    IF (OBJECT_ID('tempdb..##TABLE_DESCRIPTION') IS NOT NULL) AND (@ATABLENAME <> '')
    BEGIN
        DECLARE @KEY_NAME NVARCHAR(100)
        DECLARE @FIELD_NAME NVARCHAR(50)
        DECLARE @PARAM_FIELDS NVARCHAR(1000)
        DECLARE @UPDATE_FIELDS NVARCHAR(1000)
        DECLARE @COUNTER TINYINT
        DECLARE @COLUMN_NAME NVARCHAR(100)
        DECLARE @TYPE_DEF NVARCHAR(100)
        DECLARE @CMD NVARCHAR(4000)

        -- RETRIEVE PK COLUMN NAME
        SELECT @KEY_NAME = COLUMN_NAME FROM ##TABLE_DESCRIPTION WHERE IS_KEY = 1
        SET @PARAM_FIELDS = ''
        SET @UPDATE_FIELDS = ''
        SET @COUNTER = 1

        DECLARE FIELD_LIST CURSOR FAST_FORWARD FOR SELECT COLUMN_NAME, TYPE_DEF FROM ##TABLE_DESCRIPTION
        OPEN FIELD_LIST
        FETCH NEXT FROM FIELD_LIST INTO @COLUMN_NAME, @TYPE_DEF
        WHILE @@FETCH_STATUS = 0
        BEGIN
        IF @UPDATE_FIELDS <> ''
        BEGIN
        SET @UPDATE_FIELDS = @UPDATE_FIELDS + ', '
        END
        SET @PARAM_FIELDS = @PARAM_FIELDS + '@P_' + CAST(@COUNTER AS NVARCHAR(3)) + ' ' + @TYPE_DEF + ', '
        IF @COLUMN_NAME <> @KEY_NAME
        BEGIN
        SET @UPDATE_FIELDS = @UPDATE_FIELDS + '[' + @COLUMN_NAME + '] = @P_' + CAST(@COUNTER AS NVARCHAR(3))
        END
        SET @COUNTER = @COUNTER + 1
        FETCH NEXT FROM FIELD_LIST INTO @COLUMN_NAME, @TYPE_DEF
        END
        CLOSE FIELD_LIST
        DEALLOCATE FIELD_LIST

        SET @CMD = 'CREATE PROCEDURE [dbo].[' + @ATABLENAME + '_update] '
        + @PARAM_FIELDS +
        ' @sync_force_write Int,
        @sync_min_timestamp BigInt,
        @sync_row_count Int OUTPUT AS
        BEGIN
            SET @sync_row_count = 0;
            UPDATE [' + @ATABLENAME + '] SET ' + @UPDATE_FIELDS + ' FROM
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