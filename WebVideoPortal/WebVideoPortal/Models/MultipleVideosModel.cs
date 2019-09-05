using System;
using System.Collections.Generic;
using System.Web;

namespace WebVideoPortal.Models
{
    public class MultipleVideosModel
    {
        public IEnumerable<HttpPostedFileBase> Files { get; set; }
    }
}