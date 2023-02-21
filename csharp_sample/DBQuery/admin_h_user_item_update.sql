

CREATE PROCEDURE admin_h_user_item_update(
	IN in_dbid BIGINT,
	IN in_count INT,
	IN in_evolution SMALLINT,
	IN in_star SMALLINT,
	IN in_lv INT,
	IN in_item_tier SMALLINT,
	IN in_tier_level INT,
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
	-- 운영툴에서 캐릭터 아이템 업데이트
 -- =============================================================
	UPDATE h_item SET
		count = in_count,
		evolution = in_evolution,
		star = in_star,
		lv = in_lv,
		item_tier = in_item_tier,
		tier_level = in_tier_level
	WHERE 
		dbid = in_dbid;
	-- COMMIT;
END



