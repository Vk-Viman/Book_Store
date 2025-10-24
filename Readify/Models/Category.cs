using System;
using System.Collections.Generic;

namespace Readify.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }

        public Category? Parent { get; set; }
        public ICollection<Category>? Children { get; set; }
        public ICollection<Product>? Products { get; set; }
    }
}
