-- Names sprayed on a subway wall: the underground visitor book. One tag a player a
-- wall, kept for good; nothing updates or deletes them. The name is kept as it was.
create table subway_tags (
    id bigserial primary key,
    wall text not null,
    player_id uuid not null references players (id),
    player_name text not null,
    paint bigint not null,
    at timestamptz not null default now(),
    unique (wall, player_id)
);
