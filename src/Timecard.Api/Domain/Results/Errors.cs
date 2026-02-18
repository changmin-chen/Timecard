namespace Timecard.Api.Domain.Results;

public static class Errors
{
    public static class Calendar
    {
        public static readonly Error DataMissing =
            new("calendar.data_missing", "Calendar data is missing for the requested date.", ErrorKind.Conflict, "Calendar data missing");
    }

    public static class WorkDay
    {
        public static readonly Error PunchNotFound =
            new("workday.punch_not_found", "Punch not found.", ErrorKind.NotFound, "Not found");

        public static readonly Error PunchTooFast =
            new("workday.punch_too_fast", "Too fast. Please wait before creating another punch.", ErrorKind.Validation, "Invalid punch");

        public static readonly Error InvalidPunchDate =
            new("workday.invalid_punch_date", "Changing punch date is not supported in MVP.", ErrorKind.Conflict, "Invalid punch date");

        public static readonly Error AttendanceNotFound =
            new("workday.attendance_not_found", "Attendance request not found.", ErrorKind.NotFound, "Not found");

        public static readonly Error CategoryRequired =
            new("workday.category_required", "Category is required.", ErrorKind.Validation, "Invalid request");

        public static readonly Error StartBeforeEnd =
            new("workday.start_before_end", "Start must be before End.", ErrorKind.Validation, "Invalid request");

        public static readonly Error Overlap =
            new("workday.overlap", "Attendance request overlaps with an existing one.", ErrorKind.Conflict, "Overlap");

        public static readonly Error HasGap =
            new("workday.gap", "There is a gap between time segments. Attendance requests must be contiguous with punch time.",
                ErrorKind.Conflict, "Gap");
    }
}
