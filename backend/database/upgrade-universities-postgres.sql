-- Add university ownership to an existing Lane 1 PostgreSQL database.
-- Safe to run more than once; existing records are assigned to Default University.
begin;

create table if not exists universities (
    id            serial primary key,
    name          text not null,
    country       text not null,
    state         text not null,
    district      text not null,
    city          text not null,
    contact_email text not null,
    created_at    timestamptz not null default now()
);

insert into universities (id, name, country, state, district, city, contact_email)
values (1, 'Default University', 'India', 'Maharashtra', 'Kolhapur', 'Kolhapur', 'admin@default.university')
on conflict (id) do nothing;
select setval(pg_get_serial_sequence('universities', 'id'), greatest((select max(id) from universities), 1), true);
create unique index if not exists uq_universities_name on universities (lower(name));

alter table teachers add column if not exists university_id int not null default 1;
alter table classes add column if not exists university_id int not null default 1;
alter table subjects add column if not exists university_id int not null default 1;
alter table periods add column if not exists university_id int not null default 1;
alter table rooms add column if not exists university_id int not null default 1;
alter table lectures add column if not exists university_id int not null default 1;

do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'fk_teachers_university') then
        alter table teachers add constraint fk_teachers_university foreign key (university_id) references universities(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_classes_university') then
        alter table classes add constraint fk_classes_university foreign key (university_id) references universities(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_subjects_university') then
        alter table subjects add constraint fk_subjects_university foreign key (university_id) references universities(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_periods_university') then
        alter table periods add constraint fk_periods_university foreign key (university_id) references universities(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_rooms_university') then
        alter table rooms add constraint fk_rooms_university foreign key (university_id) references universities(id);
    end if;
    if not exists (select 1 from pg_constraint where conname = 'fk_lectures_university') then
        alter table lectures add constraint fk_lectures_university foreign key (university_id) references universities(id);
    end if;
end $$;

alter table teachers drop constraint if exists teachers_full_name_key;
alter table teachers drop constraint if exists uq_teachers_name;
alter table teachers add constraint uq_teachers_name unique (university_id, full_name);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uq_teachers_tenant_id') then
        alter table teachers add constraint uq_teachers_tenant_id unique (university_id, id);
    end if;
end $$;

alter table classes drop constraint if exists classes_name_key;
alter table classes drop constraint if exists uq_classes_name;
alter table classes add constraint uq_classes_name unique (university_id, name);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uq_classes_tenant_id') then
        alter table classes add constraint uq_classes_tenant_id unique (university_id, id);
    end if;
end $$;

alter table subjects drop constraint if exists subjects_name_key;
alter table subjects drop constraint if exists uq_subjects_name;
alter table subjects add constraint uq_subjects_name unique (university_id, name);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uq_subjects_tenant_id') then
        alter table subjects add constraint uq_subjects_tenant_id unique (university_id, id);
    end if;
end $$;

alter table periods drop constraint if exists periods_label_key;
alter table periods drop constraint if exists uq_periods_label;
alter table periods add constraint uq_periods_label unique (university_id, label);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uq_periods_tenant_id') then
        alter table periods add constraint uq_periods_tenant_id unique (university_id, id);
    end if;
end $$;

alter table rooms drop constraint if exists uq_rooms_floor_room;
alter table rooms add constraint uq_rooms_floor_room unique (university_id, floor_number, room_number);
do $$ begin
    if not exists (select 1 from pg_constraint where conname = 'uq_rooms_tenant_id') then
        alter table rooms add constraint uq_rooms_tenant_id unique (university_id, id);
    end if;
end $$;

alter table lectures drop constraint if exists uq_class_period;
alter table lectures add constraint uq_class_period unique (university_id, class_id, day_of_week, period_id);
alter table lectures drop constraint if exists uq_teacher_period;
alter table lectures add constraint uq_teacher_period unique (university_id, teacher_id, day_of_week, period_id);
alter table lectures drop constraint if exists fk_lectures_teacher_university;
alter table lectures add constraint fk_lectures_teacher_university foreign key (university_id, teacher_id) references teachers(university_id, id);
alter table lectures drop constraint if exists fk_lectures_class_university;
alter table lectures add constraint fk_lectures_class_university foreign key (university_id, class_id) references classes(university_id, id);
alter table lectures drop constraint if exists fk_lectures_subject_university;
alter table lectures add constraint fk_lectures_subject_university foreign key (university_id, subject_id) references subjects(university_id, id);
alter table lectures drop constraint if exists fk_lectures_period_university;
alter table lectures add constraint fk_lectures_period_university foreign key (university_id, period_id) references periods(university_id, id);
alter table lectures drop constraint if exists fk_lectures_room_university;
alter table lectures add constraint fk_lectures_room_university foreign key (university_id, room_id) references rooms(university_id, id);
drop index if exists uq_room_period;
create unique index uq_room_period on lectures (university_id, room_id, day_of_week, period_id) where room_id is not null;

commit;
