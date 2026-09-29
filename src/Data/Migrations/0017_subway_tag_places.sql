-- Where each tag is on its wall, in metres across and up the wall's face, its slant in
-- degrees and its font size. Chosen by the server when the tag is sprayed, at the free
-- spot nearest the player, and written once. Null for tags sprayed before this; the
-- server places those when it loads the wall.
alter table subway_tags add column x real;
alter table subway_tags add column y real;
alter table subway_tags add column angle real;
alter table subway_tags add column size integer;
