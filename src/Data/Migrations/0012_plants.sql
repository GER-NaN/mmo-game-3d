-- House plants players made in the greenhouse: unique things with provenance, kept
-- forever. The number is the plant's public identity ("Plant #12"). The creator's name
-- is kept as it was, beside their id. The design rebuilds the plant (see PlantDesign).
create table plants (
    id bigserial primary key,
    created_by uuid not null references players (id),
    creator_name text not null,
    name text not null default '',
    design text not null,
    created_at timestamptz not null default now()
);

-- Every event in a plant's life, in order: made, put on display, taken off it; later
-- sold, moved, given.
create table plant_history (
    id bigserial primary key,
    plant_id bigint not null references plants (id),
    at timestamptz not null default now(),
    event text not null
);

create index plant_history_plant on plant_history (plant_id, id);
