-- Skills (experience per skill) and the rest of a player's progress: career, time
-- played, missions. Skill and career ids are the enum numbers in Rules/Skills.
create table player_skills (
    player_id uuid not null references players (id),
    skill integer not null,
    xp bigint not null,
    primary key (player_id, skill)
);

create table player_progress (
    player_id uuid primary key references players (id),
    career integer null,
    career_xp bigint not null default 0,
    career_rank integer not null default 0,
    class_taken boolean not null default false,
    college text not null default '',
    seconds_played double precision not null default 0,
    missions bigint not null default 0
);
