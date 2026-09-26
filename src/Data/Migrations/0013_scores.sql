-- Results of objectives (the code cracker first), for their leaderboards: the top ten
-- and each player's personal best (world.md, Defense Objectives). Every result is kept;
-- the boards take each player's best. The name is kept as it was, beside the id.
create table scores (
    id bigserial primary key,
    objective text not null,
    player_id uuid not null references players (id),
    player_name text not null,
    score integer not null,
    seconds double precision not null,
    at timestamptz not null default now()
);

create index scores_objective on scores (objective, player_id);
