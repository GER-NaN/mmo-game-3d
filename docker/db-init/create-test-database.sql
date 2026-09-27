-- Runs once, when the volume is new. The game's database is POSTGRES_DB; the tests
-- have their own, so a test run never touches the world.
create database mmo3d_test;
