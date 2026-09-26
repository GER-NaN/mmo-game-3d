-- Achievements a player has earned: each once, for good.
create table player_achievements (
    player_id uuid not null references players (id),
    achievement text not null,
    at timestamptz not null default now(),
    primary key (player_id, achievement)
);
