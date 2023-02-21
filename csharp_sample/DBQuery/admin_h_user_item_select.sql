

CREATE PROCEDURE admin_h_user_item_select(
	IN in_item_type SMALLINT,
	IN in_char_uid BIGINT,
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

 -- =============================================================
	-- 운영툴에서 캐릭터 아이템을 조회
 -- =============================================================

	SELECT
	-- ROW_BEGIN : AdminHUserItem
	dbid -- BIGINT
	,item_tid -- BIGINT
	,item_type -- SMALLINT
	,count -- INT
	,evolution -- SMALLINT
	,star -- SMALLINT
	,lv -- INT
	,item_tier -- SMALLINT
	,tier_level -- INT
	,exp -- INT
	,hp -- INT
	,deck_no -- SMALLINT
	,hp_recovery_count -- INT
	,ads_hp_recovery_count -- INT
	,create_at -- DATETIME
	-- ROW_END
	FROM h_item
	WHERE 
		((in_item_type > 0 and item_type = in_item_type) or (in_item_type = 0 and item_type > 0)) and
		char_uid = in_char_uid;
	-- COMMIT;
END



