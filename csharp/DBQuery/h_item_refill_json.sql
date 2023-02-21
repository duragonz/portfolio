

CREATE PROCEDURE h_item_refill_json(
	IN in_char_uid BIGINT,
	IN in_json_item LONGTEXT,
	OUT return_value INT
)
_MAIN : BEGIN

	DECLARE EXIT HANDLER FOR SQLEXCEPTION
	BEGIN
		SET return_value = -1;
		-- ROLLBACK;
		RESIGNAL;
	END;

	 set return_value = IFNULL(return_value, 0);
	-- START TRANSACTION;
 
-- ======================
-- Item refill 일괄 업데이트
-- Refill과 Reset이 동시에 있는 경우는 Reset을 해주는게 맞다.
--	* Json_item 정보에서 Count 를 증가시킨다. (증감 처리)
-- ======================
	UPDATE h_item AS target_table_as
    INNER JOIN
    (
    	-- JSON_PARAMS_BEGIN : h_info.InstantItem
		SELECT * FROM JSON_TABLE(in_json_item, '$[*]'
		COLUMNS(
            Db_id bigint	PATH '$.Db_id' --
			, Item_tid	bigint	PATH '$.Item_tid' --
            , Count int PATH '$.Count' --
			, NextRefillTime varchar(32) PATH '$.NextRefillTime' --
			) 
		) AS json_tmp
		-- JSON_PARAMS_END 		
	) AS jt 
	
    ON 
    	target_table_as.dbid = jt.Db_id
	SET
        target_table_as.count = target_table_as.count + jt.Count
        ,target_table_as.nextRefill = jt.NextRefillTime
	WHERE 1=1;

	-- COMMIT;
END



