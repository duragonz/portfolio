CREATE TABLE `h_item`(
`dbid` BIGINT  AUTO_INCREMENT PRIMARY KEY  NOT NULL,
`char_uid` BIGINT    NOT NULL,
`item_tid` BIGINT    NOT NULL,
`item_type` SMALLINT   DEFAULT 0 NOT NULL,
`count` INT   DEFAULT 0 NOT NULL,
`lv` INT   DEFAULT 0 NOT NULL,
`exp` INT   DEFAULT 0 NOT NULL,
`item_tier` SMALLINT   DEFAULT 0 NOT NULL,
`tier_level` INT   DEFAULT 0 NOT NULL,
`nextRefill` VARCHAR (32)  DEFAULT '' NULL,
`nextReset` VARCHAR (32)  DEFAULT '' NULL,
`lock` BIT   DEFAULT b'0' NOT NULL,
`expired` BIT   DEFAULT b'0' NOT NULL,
`expireDate` DATETIME    NULL,
`create_at` DATETIME   DEFAULT utc_timestamp() NOT NULL,
`enchant_guard` INT   DEFAULT 0 NOT NULL,
`enchant_power` INT   DEFAULT 0 NOT NULL,
`hp_recovery_count` INT   DEFAULT 0 NOT NULL,
`ads_hp_recovery_count` INT   DEFAULT 0 NOT NULL,
`hp` INT   DEFAULT 0 NOT NULL,
`star` SMALLINT   DEFAULT 0 NOT NULL,
`evolution` SMALLINT   DEFAULT 0 NOT NULL,
`deck_no` SMALLINT   DEFAULT 0 NOT NULL,
`battle_attribute` SMALLINT   DEFAULT 0 NOT NULL,
`base_grade` SMALLINT   DEFAULT 0 NOT NULL
)
ENGINE = INNODB,
CHARACTER SET utf8mb4,
COLLATE utf8mb4_general_ci;

create index `ix_h_item_char_uid` on `h_item` (`char_uid`);
