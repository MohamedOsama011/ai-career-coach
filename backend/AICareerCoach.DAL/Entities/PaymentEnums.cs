using System.ComponentModel.DataAnnotations;

namespace AICareerCoach.DAL.Entities
{
    /// <summary>
    /// Lifecycle state of a <see cref="UserSubscription"/> record. The
    /// pending → active transition happens via the Fawaterak success
    /// webhook; cancelled is set by the user from the My Subscriptions
    /// page; expired is reserved for a future background reaper that
    /// will mark stale <see cref="SubscriptionStatus.Active"/> rows once
    /// their <c>EndDate</c> has passed.
    /// </summary>
    public enum SubscriptionStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Active")]
        Active,

        [Display(Name = "Cancelled")]
        Cancelled,

        /// <summary>Reserved for a future background reaper. Not assigned
        /// by any current code path.</summary>
        [Display(Name = "Expired")]
        Expired,
    }

    /// <summary>
    /// Outcome of a single Fawaterak <see cref="Payment"/> attempt.
    /// pending → paid happens via the success webhook; pending → failed
    /// is reserved for a future webhook failure path.
    /// </summary>
    public enum PaymentStatus
    {
        [Display(Name = "Pending")]
        Pending,

        [Display(Name = "Paid")]
        Paid,

        [Display(Name = "Failed")]
        Failed,
    }
}
