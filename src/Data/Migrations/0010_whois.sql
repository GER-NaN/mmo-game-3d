-- Whois, each player's page: the Plan (a short free text) and what the owner shows;
-- and props, one per visitor per page, which the visitor can take back.
create table player_pages (
    player_id uuid primary key references players (id),
    plan text not null default '',
    show_skills boolean not null default false,
    show_location boolean not null default true
);

create table props (
    giver_id uuid not null references players (id),
    receiver_id uuid not null references players (id),
    given_at timestamptz not null default now(),
    primary key (giver_id, receiver_id)
);
