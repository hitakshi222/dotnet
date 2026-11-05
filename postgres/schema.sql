-- Timetable Stack Challenge: shared schema, PostgreSQL 17 version.
-- Lane 1 stores all academic data under a university tenant.

create table universities (
    id            serial primary key,
    name          text not null,
    country       text not null,
    state         text not null,
    district      text not null,
    city          text not null,
    contact_email text not null,
    created_at    timestamptz not null default now()
);

create unique index uq_universities_name on universities (lower(name));

insert into universities (id, name, country, state, district, city, contact_email)
values (1, 'Default University', 'India', 'Maharashtra', 'Kolhapur', 'Kolhapur', 'admin@default.university');
select setval(pg_get_serial_sequence('universities', 'id'), 1, true);

create table teachers (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    full_name     text not null,
    constraint uq_teachers_name unique (university_id, full_name),
    constraint uq_teachers_tenant_id unique (university_id, id)
);

create table classes (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    name          text not null, -- e.g. '8 A'
    constraint uq_classes_name unique (university_id, name),
    constraint uq_classes_tenant_id unique (university_id, id)
);

create table subjects (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    name          text not null,
    constraint uq_subjects_name unique (university_id, name),
    constraint uq_subjects_tenant_id unique (university_id, id)
);

-- Each university defines its own school day.
create table periods (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    label         text not null,
    starts_at     time not null,
    ends_at       time not null,
    check (ends_at > starts_at),
    constraint uq_periods_label unique (university_id, label),
    constraint uq_periods_tenant_id unique (university_id, id)
);

-- Rooms and other infrastructure are owned by a university.
create table rooms (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    floor_number  int not null check (floor_number >= 0),
    room_number   int not null check (room_number > 0),
    constraint uq_rooms_floor_room unique (university_id, floor_number, room_number),
    constraint uq_rooms_tenant_id unique (university_id, id)
);

create table lectures (
    id            serial primary key,
    university_id int not null default 1 references universities(id),
    teacher_id    int not null references teachers(id),
    class_id      int not null references classes(id),
    subject_id    int not null references subjects(id),
    day_of_week   smallint not null check (day_of_week between 1 and 6),
    period_id     int not null references periods(id),
    room_id       int references rooms(id),
    created_at    timestamptz not null default now(),
    constraint uq_class_period unique (university_id, class_id, day_of_week, period_id),
    constraint uq_teacher_period unique (university_id, teacher_id, day_of_week, period_id),
    constraint fk_lectures_teacher_university foreign key (university_id, teacher_id) references teachers(university_id, id),
    constraint fk_lectures_class_university foreign key (university_id, class_id) references classes(university_id, id),
    constraint fk_lectures_subject_university foreign key (university_id, subject_id) references subjects(university_id, id),
    constraint fk_lectures_period_university foreign key (university_id, period_id) references periods(university_id, id),
    constraint fk_lectures_room_university foreign key (university_id, room_id) references rooms(university_id, id)
);

create unique index uq_room_period
    on lectures (university_id, room_id, day_of_week, period_id)
    where room_id is not null;
