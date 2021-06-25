﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace TutumAdminAPI.Models
{
    public class AdminLoginModel
    {
        [Required]
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }


        public bool CorrectCredentials { get; set; }
    }
}
