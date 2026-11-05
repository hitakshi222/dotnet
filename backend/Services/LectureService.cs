using Microsoft.EntityFrameworkCore;
using api.Data;
using api.DTOs;
using api.Models;
using Npgsql;

namespace api.Services;

public class LectureService
{
    private readonly TimetableDbContext _context;

    public LectureService(TimetableDbContext context)
    {
        _context = context;
    }

    public async Task<(List<LectureDto>? SavedLectures, List<string>? Errors, List<ClashDto>? Clashes)> SaveLecturesAsync(LectureInputDto input)
    {
        var errors = ValidateInput(input);

        if (errors.Any())
            return (null, errors, null);

        var days = input.Days;

        var teacher = await _context.Teachers.FindAsync(input.TeacherId);
        var @class = await _context.Classes.FindAsync(input.ClassId);
        var subject = await _context.Subjects.FindAsync(input.SubjectId);
        var period = await _context.Periods.FindAsync(input.PeriodId);
        var room = await _context.Rooms.FindAsync(input.RoomId);

        if (teacher == null || @class == null || subject == null || period == null || room == null)
            return (null, new List<string> { "Choose valid teacher, class, subject, period, and infrastructure values." }, null);

        var clashes = await FindClashesAsync(input, days);

        if (clashes.Any())
            return (null, null, clashes);

        var lecturesToSave = new List<Lecture>();
        foreach (var day in days)
        {
            lecturesToSave.Add(new Lecture
            {
                TeacherId = input.TeacherId,
                ClassId = input.ClassId,
                SubjectId = input.SubjectId,
                DayOfWeek = day,
                PeriodId = input.PeriodId,
                RoomId = input.RoomId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Lectures.AddRange(lecturesToSave);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var savedDtos = lecturesToSave.Select(l => new LectureDto
            {
                Id = l.Id,
                TeacherId = l.TeacherId,
                TeacherName = teacher.FullName,
                ClassId = l.ClassId,
                ClassName = @class.Name,
                SubjectId = l.SubjectId,
                SubjectName = subject.Name,
                DayOfWeek = l.DayOfWeek,
                PeriodId = l.PeriodId,
                PeriodLabel = period.Label,
                RoomId = room.Id,
                RoomName = room.Name
            }).ToList();

            return (savedDtos, null, null);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } postgresException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            var constraintClashes = new List<ClashDto>();
            foreach (var day in days)
            {
                if (postgresException.ConstraintName == "uq_class_period")
                {
                    var classConflict = await _context.Lectures
                        .Include(l => l.Teacher)
                        .Include(l => l.Subject)
                        .FirstOrDefaultAsync(l =>
                            l.ClassId == input.ClassId && l.DayOfWeek == day && l.PeriodId == input.PeriodId);
                    if (classConflict != null)
                    {
                        constraintClashes.Add(new ClashDto
                        {
                            DayOfWeek = day,
                            Rule = "R2",
                            Message = $"{@class.Name} already has {classConflict.Subject.Name} with {classConflict.Teacher.FullName} on {GetDayName(day)} {period.Label}.",
                            ConflictingLectureId = classConflict.Id
                        });
                    }
                }
                else if (postgresException.ConstraintName == "uq_teacher_period")
                {
                    var teacherConflict = await _context.Lectures
                        .Include(l => l.Class)
                        .FirstOrDefaultAsync(l =>
                            l.TeacherId == input.TeacherId && l.DayOfWeek == day && l.PeriodId == input.PeriodId);
                    if (teacherConflict != null)
                    {
                        constraintClashes.Add(new ClashDto
                        {
                            DayOfWeek = day,
                            Rule = "R3",
                            Message = $"{teacher.FullName} already teaches {teacherConflict.Class.Name} on {GetDayName(day)} {period.Label}.",
                            ConflictingLectureId = teacherConflict.Id
                        });
                    }
                }
                else if (postgresException.ConstraintName == "uq_room_period")
                {
                    var roomConflict = await _context.Lectures
                        .Include(l => l.Class)
                        .FirstOrDefaultAsync(l =>
                            l.RoomId == input.RoomId && l.DayOfWeek == day && l.PeriodId == input.PeriodId);
                    if (roomConflict != null)
                    {
                        constraintClashes.Add(new ClashDto
                        {
                            DayOfWeek = day,
                            Rule = "R5",
                            Message = $"{room.Name} is already assigned to {roomConflict.Class.Name} on {GetDayName(day)} {period.Label}.",
                            ConflictingLectureId = roomConflict.Id
                        });
                    }
                }
            }

            if (constraintClashes.Count == 0)
                throw;
            return (null, null, constraintClashes);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(LectureDto? UpdatedLecture, List<string>? Errors, List<ClashDto>? Clashes)> UpdateLectureAsync(int id, LectureInputDto input)
    {
        var errors = ValidateInput(input, requireDays: false);
        if (input.Days is { Count: > 0 })
            errors.Add("Provide exactly one day when updating a lecture.");
        if (errors.Count > 0)
            return (null, errors, null);

        var lecture = await _context.Lectures
            .Include(l => l.Teacher)
            .Include(l => l.Class)
            .Include(l => l.Subject)
            .Include(l => l.Period)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lecture == null)
            return (null, new List<string> { "Lecture not found." }, null);

        var teacher = await _context.Teachers.FindAsync(input.TeacherId);
        var @class = await _context.Classes.FindAsync(input.ClassId);
        var subject = await _context.Subjects.FindAsync(input.SubjectId);
        var period = await _context.Periods.FindAsync(input.PeriodId);
        var room = await _context.Rooms.FindAsync(input.RoomId);
        if (teacher == null || @class == null || subject == null || period == null || room == null)
            return (null, new List<string> { "Choose valid teacher, class, subject, period, and infrastructure values." }, null);

        var clashes = await FindClashesAsync(input, new[] { lecture.DayOfWeek }, id);
        if (clashes.Count > 0)
            return (null, null, clashes);

        lecture.TeacherId = input.TeacherId;
        lecture.ClassId = input.ClassId;
        lecture.SubjectId = input.SubjectId;
        lecture.PeriodId = input.PeriodId;
        lecture.RoomId = input.RoomId;
        lecture.Teacher = teacher;
        lecture.Class = @class;
        lecture.Subject = subject;
        lecture.Period = period;
        lecture.Room = room;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            var day = lecture.DayOfWeek;
            _context.ChangeTracker.Clear();
            var constraintClashes = await FindClashesAsync(input, new[] { day }, id);
            if (constraintClashes.Count == 0)
                throw;
            return (null, null, constraintClashes);
        }
        return (ToDto(lecture), null, null);
    }

    public async Task<bool> DeleteLectureAsync(int id)
    {
        var lecture = await _context.Lectures.FindAsync(id);
        if (lecture == null)
            return false;
        _context.Lectures.Remove(lecture);
        await _context.SaveChangesAsync();
        return true;
    }

    public static List<string> ValidateInput(LectureInputDto input, bool requireDays = true)
    {
        var errors = new List<string>();
        if (input.TeacherId <= 0) errors.Add("Choose a teacher.");
        if (input.ClassId <= 0) errors.Add("Choose a class.");
        if (input.SubjectId <= 0) errors.Add("Choose a subject.");
        if (input.PeriodId <= 0) errors.Add("Choose a period.");
        if (input.RoomId <= 0) errors.Add("Choose an infrastructure location.");
        if (input.Days == null || (requireDays && input.Days.Count == 0))
            errors.Add("Tick at least one day.");
        else if (input.Days.Any(day => day < 1 || day > 6))
            errors.Add("Days must be between Monday and Saturday.");
        else if (input.Days.Distinct().Count() != input.Days.Count)
            errors.Add("A day can only be selected once.");
        return errors;
    }

    private async Task<List<ClashDto>> FindClashesAsync(LectureInputDto input, IEnumerable<int> days, int? exceptLectureId = null)
    {
        var @class = await _context.Classes.FindAsync(input.ClassId);
        var teacher = await _context.Teachers.FindAsync(input.TeacherId);
        var period = await _context.Periods.FindAsync(input.PeriodId);
        var room = await _context.Rooms.FindAsync(input.RoomId);
        if (@class == null || teacher == null || period == null || room == null)
            return new List<ClashDto>();

        var requestedDays = days.Distinct().ToArray();
        var conflicts = await _context.Lectures
            .Include(l => l.Teacher)
            .Include(l => l.Class)
            .Include(l => l.Subject)
            .Include(l => l.Room)
            .Where(l => (exceptLectureId == null || l.Id != exceptLectureId.Value) &&
                requestedDays.Contains(l.DayOfWeek) && l.PeriodId == input.PeriodId)
            .ToListAsync();

        return BuildClashes(input, requestedDays, @class.Name, teacher.FullName, period.Label, room.Name, conflicts);
    }

    public static List<ClashDto> BuildClashes(
        LectureInputDto input,
        IEnumerable<int> days,
        string className,
        string teacherName,
        string periodLabel,
        string roomName,
        IEnumerable<Lecture> existingLectures)
    {
        var clashes = new List<ClashDto>();
        foreach (var day in days)
        {
            var conflicts = existingLectures
                .Where(l => l.DayOfWeek == day && l.PeriodId == input.PeriodId)
                .ToList();
            var classConflict = conflicts.FirstOrDefault(l => l.ClassId == input.ClassId);
            if (classConflict != null)
            {
                clashes.Add(new ClashDto
                {
                    DayOfWeek = day,
                    Rule = "R2",
                    Message = $"{className} already has {classConflict.Subject.Name} with {classConflict.Teacher.FullName} on {GetDayName(day)} {periodLabel}.",
                    ConflictingLectureId = classConflict.Id
                });
            }
            var teacherConflict = conflicts.FirstOrDefault(l => l.TeacherId == input.TeacherId);
            if (teacherConflict != null)
            {
                clashes.Add(new ClashDto
                {
                    DayOfWeek = day,
                    Rule = "R3",
                    Message = $"{teacherName} already teaches {teacherConflict.Class.Name} on {GetDayName(day)} {periodLabel}.",
                    ConflictingLectureId = teacherConflict.Id
                });
            }
            var roomConflict = conflicts.FirstOrDefault(l => l.RoomId == input.RoomId);
            if (roomConflict != null)
            {
                clashes.Add(new ClashDto
                {
                    DayOfWeek = day,
                    Rule = "R5",
                    Message = $"{roomName} is already assigned to {roomConflict.Class.Name} on {GetDayName(day)} {periodLabel}.",
                    ConflictingLectureId = roomConflict.Id
                });
            }
        }
        return clashes;
    }

    private static LectureDto ToDto(Lecture lecture) => new()
    {
        Id = lecture.Id,
        TeacherId = lecture.TeacherId,
        TeacherName = lecture.Teacher.FullName,
        ClassId = lecture.ClassId,
        ClassName = lecture.Class.Name,
        SubjectId = lecture.SubjectId,
        SubjectName = lecture.Subject.Name,
        DayOfWeek = lecture.DayOfWeek,
        PeriodId = lecture.PeriodId,
        PeriodLabel = lecture.Period.Label,
        RoomId = lecture.RoomId,
        RoomName = lecture.Room?.Name
    };

    public async Task<List<LectureDto>> GetTimetableAsync(int? classId, int? teacherId)
    {
        var query = _context.Lectures
            .Include(l => l.Teacher)
            .Include(l => l.Class)
            .Include(l => l.Subject)
            .Include(l => l.Period)
            .Include(l => l.Room)
            .AsQueryable();

        if (classId.HasValue)
            query = query.Where(l => l.ClassId == classId.Value);

        if (teacherId.HasValue)
            query = query.Where(l => l.TeacherId == teacherId.Value);

        var lectures = await query
            .OrderBy(l => l.DayOfWeek)
            .ThenBy(l => l.Period.StartsAt)
            .ToListAsync();

        return lectures.Select(l => new LectureDto
        {
            Id = l.Id,
            TeacherId = l.TeacherId,
            TeacherName = l.Teacher.FullName,
            ClassId = l.ClassId,
            ClassName = l.Class.Name,
            SubjectId = l.SubjectId,
            SubjectName = l.Subject.Name,
            DayOfWeek = l.DayOfWeek,
            PeriodId = l.PeriodId,
            PeriodLabel = l.Period.Label,
            RoomId = l.RoomId,
            RoomName = l.Room == null ? null : $"Floor {l.Room.FloorNumber}, Room {l.Room.RoomNumber}"
        }).ToList();
    }

    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }
}
