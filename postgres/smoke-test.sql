-- Proves the PostgreSQL database loaded correctly and enforces the same rules as SQL Server.
-- Runs in CI; nothing it inserts is kept.

do $$
begin
    if (select count(*) from lectures) <> 12 then
        raise exception 'Expected 12 seed lectures.';
    end if;

    if (select id from teachers where full_name = 'Neha Fixture') <> 6
       or (select id from periods where label = 'P6') <> 6 then
        raise exception 'Seed ids do not match the SQL Server seed.';
    end if;

    -- R2: 8 A already has Mathematics on Monday P1.
    begin
        insert into lectures (teacher_id, class_id, subject_id, day_of_week, period_id)
        values (5, 1, 5, 1, 1);
        raise exception 'R2 class clash was not blocked.';
    exception when unique_violation then null;
    end;

    -- R3: Asha Demo already teaches 8 B on Monday P2.
    begin
        insert into lectures (teacher_id, class_id, subject_id, day_of_week, period_id)
        values (1, 3, 1, 1, 2);
        raise exception 'R3 teacher clash was not blocked.';
    exception when unique_violation then null;
    end;

    -- Day must be Monday (1) to Saturday (6).
    begin
        insert into lectures (teacher_id, class_id, subject_id, day_of_week, period_id)
        values (6, 3, 3, 7, 1);
        raise exception 'Day 7 was not rejected.';
    exception when check_violation then null;
    end;

    -- A free slot must save: Meera Test, 9 A, English, Monday P4. Then undo it.
    begin
        insert into lectures (teacher_id, class_id, subject_id, day_of_week, period_id)
        values (3, 3, 3, 1, 4);
        raise exception using errcode = 'P0099', message = 'undo';
    exception when sqlstate 'P0099' then null;
    end;

    raise notice 'PostgreSQL smoke test passed.';
end $$;
