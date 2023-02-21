

CREATE PROCEDURE h_item_upsert_json(
	IN in_char_uid BIGINT,
	IN in_json_item LONGTEXT,
	OUT return_value INT
)
_MAIN : BEGIN
	
	DECLARE v_cur_date DATETIME DEFAULT(UTC_TIMESTAMP());
	DECLARE v_in_char_uid BIGINT DEFAULT(3071082);
	DECLARE v_in_money_json LONGTEXT DEFAULT('[');
	

	DECLARE EXIT HANDLER FOR SQLEXCEPTION
	BEGIN
		SET return_value = -1;
		-- ROLLBACK;
		RESIGNAL;
	END;

	 set return_value = IFNULL(return_value, 0);
	-- START TRANSACTION;
 
-- ======================
-- Item 를 json으로 받아서 일괄 처리
-- IN: h_info.InstantItem > in_json_item_infos
-- ======================
	
-- ======================
-- process
-- ======================
	INSERT INTO h_item(
		dbid, 
		char_uid, 
		item_tid, 
		item_type, 
		count, 
		lv, 
		exp, 
		deck_no,
		nextRefill,
		nextReset,
		expireDate,
		create_at
    )
    -- JSON_PARAMS_BEGIN : h_info.InstantItem
    SELECT 
		Db_id, 
		in_char_uid,
		jt.Item_tid,
		jt.ItemType,
		jt.Count,
		jt.Lv,
		jt.Exp,
		jt.DeckNo,
		jt.NextRefillTime,
		jt.NextResetTime,
		CASE WHEN jt.ExpireDate = '' THEN NULL
			ELSE jt.ExpireDate
		END,
		v_cur_date
	
	FROM JSON_TABLE(in_json_item, '$[*]'
    COLUMNS(
	              Db_id bigint	 PATH '$.Db_id' --
				, Item_tid	bigint	 PATH '$.Item_tid' --
				, ItemType smallint	 PATH '$.ItemType' -- h_define::item_type_e
				, Count  int	 PATH '$.Count' -- 
				, Lv int	 PATH '$.Lv' --  
				, Exp int	 PATH '$.Exp' --  
				, DeckNo smallint  PATH '$.DeckNo' --
				, NextRefillTime varchar(32)  PATH '$.NextRefillTime' --
				, NextResetTime varchar(32)  PATH '$.NextResetTime' --
				, ExpireDate varchar(32)  PATH '$.ExpireDate' --
	
    ) ) AS jt
    -- JSON_PARAMS_END 
    ON DUPLICATE KEY UPDATE
		h_item.count = h_item.count + jt.Count
        ,h_item.exp = h_item.exp + jt.Exp;
	
	call h_profile_img_upsert_json(in_char_uid, in_json_item, return_value);

	-- COMMIT;
END



