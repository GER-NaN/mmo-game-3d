-- What the player looks like, chosen once at creation. Existing players get the default.
alter table players add column look text not null default 'a';
