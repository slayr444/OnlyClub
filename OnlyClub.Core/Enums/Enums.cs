namespace OnlyClub.Core.Enums;

public enum TableStatus { Free, Held, Confirmed, Seated }
public enum BookingStatus { Held, Confirmed, Seated, Cancelled, NoShow }
public enum GuestListStatus { Registered, Confirmed, CheckedIn, Cancelled, NoShow }
public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    RefundRequired,
    Refunded,
    RefundFailed,
    Cancelled,
}
public enum CommissionStatus { Pending, Approved, Paid }
public enum ScheduledMessageStatus { Pending, Sent, Cancelled, Failed }

public enum ScheduledMessageType
{
    UnpaidHoldReminder,
    BookingConfirmation,
    HoldExpired,                    
    DayBeforeReminder,
    NextEventAnnouncement,
}

public enum Role { Guest, Promoter, Guard, Manager }

public enum AuditAction
{
    BookingSeat,
    CheckIn,
    EventClose,
    EventReopen,
    CommissionApprove,
    CommissionPay,
    BookingCancel,
    EventCreate,
}