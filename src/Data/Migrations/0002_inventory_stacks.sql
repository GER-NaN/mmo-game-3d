-- What a player carries, one row per type and tier. Types and tiers are stored by
-- name, so reordering the enums in code never changes what a row means.
create table inventory_stacks (
    player_id uuid not null references players (id),
    item_type text not null,
    tier text not null,
    quantity int not null check (quantity > 0),
    primary key (player_id, item_type, tier)
);
