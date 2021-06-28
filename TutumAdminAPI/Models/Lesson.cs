using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TutumAdminAPI.Models
{
    public partial class Lesson
    {
        public int LessonId { get; set; }

        [Display(Name = "Курс")]
        public int CourseId { get; set; }

        [Required]
        [Display(Name = "Название")]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Текст")]
        [MaxLength(1000)]
        public string Text { get; set; }

        public string VideoPath { get; set; }

        public string PreviewPath { get; set; }

        [Display(Name = "Курс")]
        [JsonIgnore]
        public virtual Course Course { get; set; }

        public bool ShouldSerializeCourseId() => false;
        public bool ShouldSerializeText() => ShowAllData;
        public bool ShouldSerializeVideoPath() => ShowAllData;

        [JsonIgnore]
        [NotMapped]
        public bool ShowAllData = false;

        [Required]
        [Display(Name = "Видео файл")]
        [NotMapped]
        public string VideoFileName { get; set; }
    }
}
