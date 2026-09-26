-- The state of the world that is not any one player's: the street lights, and later
-- the rest of what players change. One row per thing, by a readable key.
create table world_state (
    key text primary key,
    value text not null,
    updated_at timestamptz not null default now()
);

-- Who did what to a town, and when: the terminal's town log.
create table town_log (
    id bigserial primary key,
    zone text not null,
    entry text not null,
    at timestamptz not null default now()
);

create index town_log_zone_at on town_log (zone, at desc);
