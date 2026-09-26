-- Pocket change. A new player has $10; existing players get the same.
alter table players add column dollars int not null default 10 check (dollars >= 0);
