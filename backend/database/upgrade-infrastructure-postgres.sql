BEGIN;

CREATE TABLE IF NOT EXISTS rooms (
    id           serial primary key,
    floor_number int not null check (floor_number >= 0),
    room_number  int not null check (room_number > 0),
    constraint uq_rooms_floor_room unique (floor_number, room_number)
);

INSERT INTO rooms (floor_number, room_number)
SELECT floor_number, room_number
FROM (VALUES
    (1, 1), (1, 2), (1, 3),
    (2, 1), (2, 2), (2, 3),
    (3, 1), (3, 2), (3, 3),
    (4, 1), (4, 2), (4, 3)
) AS seed(floor_number, room_number)
WHERE NOT EXISTS (
    SELECT 1
    FROM rooms existing
    WHERE existing.floor_number = seed.floor_number
      AND existing.room_number = seed.room_number
);

ALTER TABLE lectures
    ADD COLUMN IF NOT EXISTS room_id int;

UPDATE lectures AS lecture
SET room_id = room.id
FROM rooms AS room,
(VALUES
    (1, 1, 1, 1, 1, 1, 1),
    (2, 1, 2, 1, 2, 1, 1),
    (3, 2, 3, 1, 1, 1, 2),
    (1, 2, 1, 1, 2, 1, 2),
    (4, 3, 4, 1, 1, 1, 3),
    (3, 1, 3, 2, 1, 1, 1),
    (2, 2, 2, 2, 1, 1, 2),
    (1, 3, 1, 2, 2, 1, 1),
    (1, 1, 1, 3, 1, 1, 1),
    (5, 3, 5, 3, 3, 1, 1),
    (4, 2, 4, 3, 3, 1, 2),
    (1, 1, 1, 4, 1, 1, 1)
) AS seed(teacher_id, class_id, subject_id, day_of_week, period_id, floor_number, room_number)
WHERE lecture.teacher_id = seed.teacher_id
    AND lecture.class_id = seed.class_id
    AND lecture.subject_id = seed.subject_id
    AND lecture.day_of_week = seed.day_of_week
    AND lecture.period_id = seed.period_id
    AND room.floor_number = seed.floor_number
    AND room.room_number = seed.room_number
    AND lecture.room_id IS NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_lectures_room'
          AND conrelid = 'lectures'::regclass
    ) THEN
        ALTER TABLE lectures
            ADD CONSTRAINT fk_lectures_room
            FOREIGN KEY (room_id) REFERENCES rooms(id);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS uq_room_period
    ON lectures (room_id, day_of_week, period_id)
    WHERE room_id IS NOT NULL;

COMMIT;
