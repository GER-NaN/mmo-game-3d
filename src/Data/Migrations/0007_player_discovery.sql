-- Where each player has been, per zone, for the map: a bit set of the zone's cells.
create table player_discovery (
    player_id uuid not null references players (id),
    zone text not null,
    cells bytea not null,
    primary key (player_id, zone)
);
