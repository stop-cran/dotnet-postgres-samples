using System;

namespace PostgresSamples.Model
{
    public class Child2Entity
    {
        public int Id { get; set; }
        public DateTime Modified { get; set; }
        public int ParentId { get; set; }
        public ParentEntity Parent { get; set; }
    }
}