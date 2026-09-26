-- An account has characters in slots (world.md: two, more can be bought), not one
-- player. Players made before this are in slot 0.
alter table players drop constraint if exists players_account_id_key;
alter table players add column slot integer not null default 0;
alter table players add constraint players_account_slot unique (account_id, slot);
