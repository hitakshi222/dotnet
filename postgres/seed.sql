-- Synthetic seed data. Every name here is made up.
-- Same rows and ids as db/sqlserver/seed.sql.

insert into teachers (full_name) values
  ('Asha Demo'),      -- 1
  ('Rohan Sample'),   -- 2
  ('Meera Test'),     -- 3
  ('Kiran Example'),  -- 4
  ('Vikram Mock'),    -- 5
  ('Neha Fixture');   -- 6, no lectures yet: shows the report's empty state

insert into classes (name) values
  ('8 A'),            -- 1
  ('8 B'),            -- 2
  ('9 A');            -- 3

insert into subjects (name) values
  ('Mathematics'),    -- 1
  ('Science'),        -- 2
  ('English'),        -- 3
  ('Marathi'),        -- 4
  ('History');        -- 5

insert into periods (label, starts_at, ends_at) values
  ('P1', '09:00', '09:45'),
  ('P2', '09:45', '10:30'),
  ('P3', '10:45', '11:30'),
  ('P4', '11:30', '12:15'),
  ('P5', '13:00', '13:45'),
  ('P6', '13:45', '14:30');

insert into rooms (floor_number, room_number) values
  (1, 1), (1, 2), (1, 3),
  (2, 1), (2, 2), (2, 3),
  (3, 1), (3, 2), (3, 3),
  (4, 1), (4, 2), (4, 3);

-- A partly filled week, so the report has something to show.
insert into lectures (teacher_id, class_id, subject_id, day_of_week, period_id, room_id) values
  -- Monday
  (1, 1, 1, 1, 1, 1),   -- Asha Demo,     8 A, Mathematics, P1, Floor 1 Room 1
  (2, 1, 2, 1, 2, 1),   -- Rohan Sample,  8 A, Science,     P2, Floor 1 Room 1
  (3, 2, 3, 1, 1, 2),   -- Meera Test,    8 B, English,     P1, Floor 1 Room 2
  (1, 2, 1, 1, 2, 2),   -- Asha Demo,     8 B, Mathematics, P2, Floor 1 Room 2
  (4, 3, 4, 1, 1, 3),   -- Kiran Example, 9 A, Marathi,     P1, Floor 1 Room 3
  -- Tuesday
  (3, 1, 3, 2, 1, 1),   -- Meera Test,    8 A, English,     P1
  (2, 2, 2, 2, 1, 2),   -- Rohan Sample,  8 B, Science,     P1
  (1, 3, 1, 2, 2, 1),   -- Asha Demo,     9 A, Mathematics, P2
  -- Wednesday
  (1, 1, 1, 3, 1, 1),   -- Asha Demo,     8 A, Mathematics, P1
  (5, 3, 5, 3, 3, 1),   -- Vikram Mock,   9 A, History,     P3
  (4, 2, 4, 3, 3, 2),   -- Kiran Example, 8 B, Marathi,     P3
  -- Thursday
  (1, 1, 1, 4, 1, 1);   -- Asha Demo,     8 A, Mathematics, P1
