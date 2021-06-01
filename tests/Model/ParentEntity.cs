using System.Collections.Generic;

namespace PostgresSamples.Model
{
    public class ParentEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Child1Entity> Children1 { get; set; }
        public ICollection<Child2Entity> Children2 { get; set; }
    }
}