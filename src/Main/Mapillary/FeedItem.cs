using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapillary
{
    public class FeedItem
    {
        public string Action { get; set; }

        public string Type { get; set; }

        public string ObjectType { get; set; }

        public string ObjectKey { get; set; }

        public string ObjectValue { get; set; }

        public string ImageUrl { get; set; }

        public string ImageKey { get; set; }

        public string MainDescription { get; set; }

        public string DetailDescription { get; set; }

        public DateTime CreatedAt { get; set; }

        public string StartedAt { get; set; }

        public string UpdatedAt { get; set; }

        public string Id { get; set; }

        public int NbrObjects { get; set; }

        public string User { get; set; }

        public bool Open { get; set; }

        public string FirstLine { get; set; }

        public string SecondLine { get; set; }
    }
}
