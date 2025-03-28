﻿using System.ComponentModel.DataAnnotations;

namespace Auth.Domain.Entities
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
