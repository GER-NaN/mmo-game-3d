-- Things with an identity: this phone, this battery at 43%. An instance is loose in
-- the bag (no parent, no slot), equipped by its player (no parent, a slot), or inside
-- another instance (a parent and the slot it fills there). Saved by replace per player,
-- like the stacks.
create table item_instances (
    id uuid primary key,
    player_id uuid not null references players (id),
    parent_id uuid,
    item_type text not null,
    tier text not null,
    slot text,
    charge real check (charge >= 0 and charge <= 1)
);

create index item_instances_player on item_instances (player_id);
