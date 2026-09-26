-- Each player's friends and ignored players. One-way: a row says what the owner chose,
-- nothing about the other player. A player is on at most one of the two lists.
create table contacts (
    player_id uuid not null references players (id),
    contact_id uuid not null references players (id),
    ignored boolean not null,
    created_at timestamptz not null default now(),
    primary key (player_id, contact_id)
);
