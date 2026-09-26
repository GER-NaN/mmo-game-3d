-- An account is who a player is; the license key is how this copy of the game proves
-- it, until real auth replaces the key with a login.
create table accounts (
    id uuid primary key,
    license_key uuid not null unique,
    created_at timestamptz not null default now()
);

-- One character per account for now. A player always stands somewhere, so a new row
-- is written with the zone's spawn. Positions are world units: y is up.
create table players (
    id uuid primary key,
    account_id uuid not null unique references accounts (id),
    display_name text not null,
    zone text not null,
    position_x real not null,
    position_y real not null,
    position_z real not null,
    yaw real not null default 0,
    created_at timestamptz not null default now(),
    saved_at timestamptz not null default now()
);
