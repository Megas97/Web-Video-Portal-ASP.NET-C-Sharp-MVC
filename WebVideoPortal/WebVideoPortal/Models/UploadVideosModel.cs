using System;

namespace WebVideoPortal.Models
{
    public class UploadVideosModel
    {
        public int VideoID { get; set; }
        public string VideoName { get; set; }
        public string VideoAuthor { get; set; }
        public string VideoSize { get; set; }
        public string VideoUploadDate { get; set; }
        public string VideoPath { get; set; }
        public Nullable<bool> IsVideoActive { get; set; }
    }
}