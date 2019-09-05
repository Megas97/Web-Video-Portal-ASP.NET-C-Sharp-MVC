using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Data;
using WebVideoPortal.Models;
using System.Text.RegularExpressions;

namespace WebVideoPortal.Controllers
{
    public class VideoController : Controller
    {
        #region // Upload Action
        [Authorize]
        [HttpGet]
        public ActionResult Upload()
        {
            return View();
        }
        #endregion

        #region // Upload POST Action
        [Authorize]
        [HttpPost]
        public ActionResult Upload(MultipleVideosModel videoFiles)
        {
            string message = "";
            int totalSize = 0;
            int uploadedFiles = 0;
            if ((videoFiles != null) && (videoFiles.Files.Count() > 0))
            {
                foreach (var videoFile in videoFiles.Files)
                {
                    if (videoFile != null)
                    {
                        totalSize += videoFile.ContentLength;
                    }
                }
                if (totalSize <= 524288000) // 500 MB maximum for all selected files
                {
                    foreach (var videoFile in videoFiles.Files)
                    {
                        using (DatabaseEntities de = new DatabaseEntities())
                        {
                            if (videoFile != null)
                            {
                                if (Path.GetExtension(videoFile.FileName) == ".mp4")
                                {
                                    if (videoFile.ContentLength < 104857600) // 100 MB maximum per file
                                    {
                                        // We do the following because otherwise when a user deletes a video the IDs and file names will get corrupted!
                                        // We add an empty video record (with blank name & path as they cannot be null)
                                        Video video = new Video();

                                        de.Videos.Add(video);
                                        video.VideoName = "";
                                        video.VideoPath = "";
                                        de.SaveChanges();

                                        // We get the actual uploaded video file's information
                                        string videoName = Path.GetFileNameWithoutExtension(videoFile.FileName);
                                        string videoAuthor = HttpContext.User.Identity.Name;
                                        string videoSize = Math.Round(((videoFile.ContentLength / 1024f) / 1024f), 1).ToString(); // In MB
                                        string videoUploadDate = DateTime.Now.ToLongDateString();
                                        int lastIndex = video.VideoID; // We get the ID of the inserted empty video record and use it as name of file
                                        string videoPath = "/Uploads/" + lastIndex + Path.GetExtension(videoFile.FileName); // Video File Name == ID (Unique!)

                                        // We save the file & edit the empty video record with the correct values
                                        videoFile.SaveAs(Server.MapPath(videoPath));

                                        video.VideoName = videoName;
                                        video.VideoAuthor = videoAuthor;
                                        video.VideoSize = videoSize;
                                        video.VideoUploadDate = videoUploadDate;
                                        video.VideoPath = videoPath;
                                        video.IsVideoActive = true;

                                        de.SaveChanges();
                                        uploadedFiles++;

                                        if (videoFiles.Files.Count() == 1)
                                        {
                                            message = "Video uploaded successfully: " + uploadedFiles;
                                        }
                                        else
                                        {
                                            message = "Videos uploaded successfully: " + uploadedFiles;
                                        }
                                    }
                                    else
                                    {
                                        message = "Limit is 100 MB per file!";
                                    }
                                }
                                else
                                {
                                    message = "Only .mp4 video files are allowed!";
                                }
                            }
                            else
                            {
                                message = "There are no files selected!";
                            }
                        }
                    }
                }
                else
                {
                    message = message = "Limit is 500 MB for all selected files!";
                }
            }
            else
            {
                message = "There are no files selected!";
            }

            ViewBag.Message = message;
            return View();
        }
        #endregion

        #region // Display All Videos Action
        [HttpGet]
        public ActionResult DisplayAllVideos()
        {
            string message = "";
            int availableVideos = 0;
            List<UploadVideosModel> videoList = new List<UploadVideosModel>();
            using (DatabaseEntities de = new DatabaseEntities())
            {
                if (de.Videos.Count() > 0)
                {
                    foreach (var video in de.Videos)
                    {
                        if (video.IsVideoActive == true)
                        {
                            UploadVideosModel videosModel = new UploadVideosModel();
                            videosModel.VideoName = video.VideoName;
                            videosModel.VideoAuthor = video.VideoAuthor;
                            videosModel.VideoSize = video.VideoSize;
                            videosModel.VideoUploadDate = video.VideoUploadDate;
                            videosModel.VideoPath = video.VideoPath;
                            videosModel.IsVideoActive = video.IsVideoActive;
                            videoList.Add(videosModel);
                            availableVideos++;
                        }
                    }
                }
                else
                {
                    message = "There are no videos in the database!";
                }
            }

            if (availableVideos == 1)
            {
                message = "There is " + availableVideos + " video available!";
            }
            else if (availableVideos > 1)
            {
                message = "There are " + availableVideos + " videos available!";
            }
            else
            {
                message = "There are no videos available!";
            }

            ViewBag.Message = message;
            return View(videoList);
        }
        #endregion

        #region // Display My Videos Action
        [Authorize]
        [HttpGet]
        public ActionResult DisplayMyVideos()
        {
            string message = "";
            int uploadedVideos = 0;
            int publicVideos = 0;
            int privateVideos = 0;
            List<UploadVideosModel> videoList = new List<UploadVideosModel>();
            using (DatabaseEntities de = new DatabaseEntities())
            {
                if (de.Videos.Count() > 0)
                {
                    foreach (var video in de.Videos)
                    {
                        if (video.VideoAuthor == HttpContext.User.Identity.Name)
                        {
                            UploadVideosModel videosModel = new UploadVideosModel();
                            videosModel.VideoName = video.VideoName;
                            videosModel.VideoAuthor = video.VideoAuthor;
                            videosModel.VideoSize = video.VideoSize;
                            videosModel.VideoUploadDate = video.VideoUploadDate;
                            videosModel.VideoPath = video.VideoPath;
                            videosModel.IsVideoActive = video.IsVideoActive;
                            videoList.Add(videosModel);
                            uploadedVideos++;
                            if (video.IsVideoActive == true)
                            {
                                publicVideos++;
                            }
                            else
                            {
                                privateVideos++;
                            }
                        }
                    }
                }
                else
                {
                    message = "There are no videos in the database!";
                }
            }

            if (uploadedVideos == 1)
            {
                message = "You have " + uploadedVideos + " uploaded video!";
            }
            else if (uploadedVideos > 1)
            {
                message = "You have " + uploadedVideos + " uploaded videos!";
            }
            else
            {
                message = "You have no uploaded videos!";
            }

            ViewBag.Message = message;
            ViewBag.PublicVideos = publicVideos;
            ViewBag.PrivateVideos = privateVideos;
            return View(videoList);
        }
        #endregion

        #region // Download Video Action
        [HttpGet]
        public FileResult DownloadVideo(string path)
        {
            var type = "video/mp4";
            string name = "";
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var video = de.Videos.Where(a => a.VideoPath == path).FirstOrDefault();
                name = video.VideoName + Path.GetExtension(path);
            }
            return File(path, type, name);
        }
        #endregion

        #region // Hide Video Action
        [Authorize]
        [HttpGet]
        public ActionResult HideVideo(string path)
        {
            string message = "";
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var video = de.Videos.Where(a => a.VideoPath == path).FirstOrDefault();
                if (video != null)
                {
                    if (video.IsVideoActive == true)
                    {
                        if (video.VideoAuthor == HttpContext.User.Identity.Name)
                        {
                            video.IsVideoActive = false;
                            de.SaveChanges();
                            message = "Video successfully hidden!";
                        }
                        else
                        {
                            message = "Only " + video.VideoAuthor + " can hide their videos!";
                        }
                    }
                }
                else
                {
                    message = "Invalid Request!";
                }
            }

            ViewBag.Message = message;
            return Redirect(Request.UrlReferrer.ToString());
        }
        #endregion

        #region Unhide Video Action
        [Authorize]
        [HttpGet]
        public ActionResult UnhideVideo(string path)
        {
            string message = "";
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var video = de.Videos.Where(a => a.VideoPath == path).FirstOrDefault();
                if (video != null)
                {
                    if (video.IsVideoActive != true)
                    {
                        if (video.VideoAuthor == HttpContext.User.Identity.Name)
                        {
                            video.IsVideoActive = true;
                            de.SaveChanges();
                            message = "Video successfully unhidden!";
                        }
                        else
                        {
                            message = "Only " + video.VideoAuthor + " can unhide their videos!";
                        }
                    }
                }
                else
                {
                    message = "Invalid Request!";
                }
            }

            ViewBag.Message = message;
            return Redirect(Request.UrlReferrer.ToString());
        }
        #endregion

        #region Delete Video Action
        [Authorize]
        [HttpGet]
        public ActionResult DeleteVideo(string path)
        {
            string message = "";
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var video = de.Videos.Where(a => a.VideoPath == path).FirstOrDefault();
                if (video != null)
                {
                    if (video.VideoAuthor == HttpContext.User.Identity.Name)
                    {
                        if (System.IO.File.Exists(Request.MapPath(path)))
                        {
                            de.Videos.Remove(video);
                            de.SaveChanges();
                            System.IO.File.Delete(Request.MapPath(path));
                            message = "Video successfully deleted!";
                        }
                    }
                    else
                    {
                        message = "Only " + video.VideoAuthor + " can delete their videos!";
                    }
                }
            }

            ViewBag.Message = message;
            return Redirect(Request.UrlReferrer.ToString());
        }
        #endregion

        #region // Display User Videos Action
        [HttpGet]
        public ActionResult DisplayUserVideos(string emailID)
        {
            string message = "";
            int uploadedVideos = 0;
            int publicVideos = 0;
            int privateVideos = 0;
            List<UploadVideosModel> videoList = new List<UploadVideosModel>();
            if ((emailID != "") && (emailID != null))
            {
                if (IsEmail(emailID))
                {
                    using (DatabaseEntities de = new DatabaseEntities())
                    {
                        if (de.Videos.Count() > 0)
                        {
                            var user = de.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                            if (user != null)
                            {
                                ViewBag.User = user;
                                foreach (var video in de.Videos)
                                {
                                    if (video.VideoAuthor == user.EmailID)
                                    {
                                        UploadVideosModel videosModel = new UploadVideosModel();
                                        videosModel.VideoName = video.VideoName;
                                        videosModel.VideoAuthor = video.VideoAuthor;
                                        videosModel.VideoSize = video.VideoSize;
                                        videosModel.VideoUploadDate = video.VideoUploadDate;
                                        videosModel.VideoPath = video.VideoPath;
                                        videosModel.IsVideoActive = video.IsVideoActive;
                                        videoList.Add(videosModel);
                                        uploadedVideos++;
                                        if (video.IsVideoActive == true)
                                        {
                                            publicVideos++;
                                        }
                                        else
                                        {
                                            privateVideos++;
                                        }
                                    }
                                }
                                if (uploadedVideos == 0)
                                {
                                    message = user.EmailID + " has no uploaded videos!";
                                }
                                if (uploadedVideos == 1)
                                {
                                    message = user.EmailID + " has " + uploadedVideos + " uploaded video!";
                                }
                                else if (uploadedVideos > 1)
                                {
                                    message = user.EmailID + " has " + uploadedVideos + " uploaded videos!";
                                }
                                else
                                {
                                    message = user.EmailID + " has no uploaded videos!";
                                }
                            }
                            else
                            {
                                message = "No account linked to email " + emailID + " was found!";
                            }
                        }
                        else
                        {
                            message = "There are no videos in the database!";
                        }
                    }
                }
                else
                {
                    message = emailID + " is not a valid email address!";
                }
            }
            else
            {
                message = "Please input an email address!";
            }

            ViewBag.Message = message;
            ViewBag.PublicVideos = publicVideos;
            ViewBag.PrivateVideos = privateVideos;
            return View(videoList);
        }
        #endregion

        #region // Helper Functions
        [NonAction]
        public static bool IsEmail(string email)
        {
            string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
            Regex re = new Regex(strRegex);
            if (re.IsMatch(email))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion
    }
}