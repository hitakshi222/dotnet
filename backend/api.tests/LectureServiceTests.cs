using api.DTOs;
using api.Models;
using api.Services;

namespace api.tests;

public class LectureServiceTests
{
    [Fact]
    public void ValidateInput_RequiresAllLookupsAndAtLeastOneDay()
    {
        var errors = LectureService.ValidateInput(new LectureInputDto());

        Assert.Equal(6, errors.Count);
        Assert.Contains("Choose a teacher.", errors);
        Assert.Contains("Choose a class.", errors);
        Assert.Contains("Choose a subject.", errors);
        Assert.Contains("Choose a period.", errors);
        Assert.Contains("Choose an infrastructure location.", errors);
        Assert.Contains("Tick at least one day.", errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void ValidateInput_RejectsDaysOutsideMondayThroughSaturday(int invalidDay)
    {
        var errors = LectureService.ValidateInput(ValidInput(invalidDay));

        Assert.Contains("Days must be between Monday and Saturday.", errors);
    }

    [Fact]
    public void ValidateInput_RejectsDuplicateDays()
    {
        var errors = LectureService.ValidateInput(ValidInput(1, 1));

        Assert.Contains("A day can only be selected once.", errors);
    }

    [Fact]
    public void ValidateInput_RejectsNonPositiveLookupIds()
    {
        var input = ValidInput(1);
        input.TeacherId = -1;
        input.PeriodId = 0;
        input.RoomId = -3;

        var errors = LectureService.ValidateInput(input);

        Assert.Contains("Choose a teacher.", errors);
        Assert.Contains("Choose a period.", errors);
        Assert.Contains("Choose an infrastructure location.", errors);
    }

    [Fact]
    public void BuildClashes_RejectsRoomAlreadyBookedForAnotherClass()
    {
        var existingLecture = new Lecture
        {
            Id = 42,
            ClassId = 1,
            Class = new Class { Id = 1, Name = "8 A" },
            TeacherId = 1,
            Teacher = new Teacher { Id = 1, FullName = "Asha Rao" },
            SubjectId = 1,
            Subject = new Subject { Id = 1, Name = "Math" },
            DayOfWeek = 1,
            PeriodId = 2,
            Period = new Period { Id = 2, Label = "P2" },
            RoomId = 3
        };

        var clashes = LectureService.BuildClashes(
            new LectureInputDto { ClassId = 2, TeacherId = 2, PeriodId = 2, RoomId = 3 },
            new[] { 1 },
            "8 B",
            "Ravi Das",
            "P2",
            "Floor 1, Room 3",
            new[] { existingLecture });

        var clash = Assert.Single(clashes);
        Assert.Equal("R5", clash.Rule);
        Assert.Equal(1, clash.DayOfWeek);
        Assert.Equal(42, clash.ConflictingLectureId);
        Assert.Contains("Floor 1, Room 3", clash.Message);
    }

    private static LectureInputDto ValidInput(params int[] days) => new()
    {
        TeacherId = 1,
        ClassId = 1,
        SubjectId = 1,
        PeriodId = 1,
        RoomId = 1,
        Days = days.ToList()
    };
}
