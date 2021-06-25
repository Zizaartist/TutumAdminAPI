using Newtonsoft.Json;
using System;
using System.Collections.Generic;

#nullable disable

namespace TutumAdminAPI.Models
{
    public partial class Subscription
    {
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public DateTime ActivationDate { get; set; }
        public DateTime Expires { get; set; }

        [JsonIgnore]
        public virtual User User { get; set; }
    }
}
