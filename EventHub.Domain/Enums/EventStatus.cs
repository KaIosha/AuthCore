using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Domain.Enums
{
    public enum EventStatus
    {
        Pending,
        Published,
        Cancelled,
        Completed,
        Postponed,
        Finished

    }
}
