-- World events (world.md section 2): what kinds run and how often, each run, who took
-- part, and each player's world event points. A definition row is what an admin panel
-- will edit later; the numbers seeded here are placeholders.
create table world_event_definitions (
    id text primary key,
    family text not null,
    kind text not null,
    line text not null,
    zone text not null,
    spot text not null,
    count integer not null,
    time_limit_seconds integer not null,
    every_seconds integer not null,
    enabled boolean not null default true
);

insert into world_event_definitions (id, family, kind, line, zone, spot, count, time_limit_seconds, every_seconds)
values ('drone-swarm-meadows', 'ai-swarm', 'drone-swarm', 'Drone Swarm in Meadows!', 'meadows', 'DroneSwarm', 10, 180, 300);

-- A run is 'running' until it ends; then its outcome says how: 'completed',
-- 'timed-out', 'ended-by-restart'.
create table world_event_runs (
    id bigserial primary key,
    definition_id text not null references world_event_definitions (id),
    status text not null default 'running',
    started_at timestamptz not null default now(),
    ended_at timestamptz,
    outcome text
);

create index world_event_runs_status on world_event_runs (status);

create table world_event_participants (
    run_id bigint not null references world_event_runs (id),
    player_id uuid not null references players (id),
    primary key (run_id, player_id)
);

create table world_event_points (
    player_id uuid primary key references players (id),
    points integer not null default 0
);
