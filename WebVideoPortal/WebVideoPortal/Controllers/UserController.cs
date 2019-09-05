using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebVideoPortal.Models;

namespace WebVideoPortal.Controllers
{
    public class UserController : Controller
    {
        #region // Register Action
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }
        #endregion

        #region // Register POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register([Bind(Exclude = "IsEmailVerified, ActivationCode")] User user)
        {
            bool Status = false;
            string message = "";

            #region // Model Validation
            if (ModelState.IsValid)
            {
                if (IsEmail(user.EmailID))
                {
                    #region // Email already exists
                    var exists = DoesEmailExist(user.EmailID);
                    if (exists)
                    {
                        ModelState.AddModelError("EmailExists", "Email is already in use by another user!");
                        return View(user);
                    }
                    #endregion

                    #region // Generate Activation Code
                    user.ActivationCode = Guid.NewGuid();
                    #endregion

                    #region // Password Hashing
                    user.Password = Crypto.Hash(user.Password);
                    user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword); // We do this so that we compare the hashes of both passwords, not the plain text
                    #endregion

                    user.IsEmailVerified = false;

                    #region // Save Changes To Database
                    using (DatabaseEntities de = new DatabaseEntities())
                    {
                        de.Users.Add(user);
                        de.SaveChanges();

                        #region // Send Verification Email
                        SendVerificationEmail(user.EmailID, user.ActivationCode.ToString());
                        message = "Registration successfully done! Account activation link has been sent to your email at: " + user.EmailID;
                        Status = true;
                        #endregion
                    }
                    #endregion
                }
                else
                {
                    message = user.EmailID + " is not a valid email address!";
                }
            }
            else
            {
                message = "Invalid Request!";
            }
            #endregion

            ViewBag.Message = message;
            ViewBag.Status = Status;
            return View(user);
        }
        #endregion

        #region // Verify Account Action
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (DatabaseEntities de = new DatabaseEntities())
            {
                de.Configuration.ValidateOnSaveEnabled = false; // Added to avoid issue with confirm password match on save changes

                var user = de.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (user != null)
                {
                    user.IsEmailVerified = true;
                    de.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request!";
                }
            }

            ViewBag.Status = Status;
            return View();
        }
        #endregion

        #region // Login Action
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        #endregion

        #region // Login POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLoginModel userLoginModel, string ReturnUrl)
        {
            string message = "";
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var user = de.Users.Where(a => a.EmailID == userLoginModel.EmailID).FirstOrDefault();
                if (user != null)
                {
                    if (user.IsEmailVerified == true)
                    {
                        if (string.Compare(Crypto.Hash(userLoginModel.Password), user.Password) == 0)
                        {
                            int timeout = userLoginModel.RememberMe ? 525600 : 20; // 525600 mins = 1 year
                            var ticket = new FormsAuthenticationTicket(userLoginModel.EmailID, userLoginModel.RememberMe, timeout);
                            string encrypted = FormsAuthentication.Encrypt(ticket);
                            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                            cookie.Expires = DateTime.Now.AddMinutes(timeout);
                            cookie.HttpOnly = true;
                            Response.Cookies.Add(cookie);

                            if (Url.IsLocalUrl(ReturnUrl))
                            {
                                return Redirect(ReturnUrl);
                            }
                            else
                            {
                                return RedirectToAction("DisplayAllVideos", "Video");
                            }
                        }
                        else
                        {
                            message = "Invalid credentials provided!";
                        }
                    }
                    else if (user.IsEmailVerified == false) 
                    {
                        message = "You must verify your account before you can log in with it!";
                    }
                }
                else
                {
                    message = "Invalid credentials provided!";
                }
            }

            ViewBag.Message = message;
            return View();
        }
        #endregion

        #region // Logout Action
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("DisplayAllVideos", "Video");
        }
        #endregion

        #region // Forgot Password Action
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            return View();
        }
        #endregion

        #region // Forgot Password POST Action
        [HttpPost]
        public ActionResult ForgotPassword(string emailID)
        {
            string message = "";
            bool Status = false;

            if (emailID != "")
            {
                if (IsEmail(emailID))
                {
                    using (DatabaseEntities de = new DatabaseEntities())
                    {
                        var user = de.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                        if (user != null)
                        {
                            // Send email for resetting password
                            string resetCode = Guid.NewGuid().ToString();
                            SendVerificationEmail(user.EmailID, resetCode, "ResetPassword");
                            user.ResetPasswordCode = resetCode;
                            de.Configuration.ValidateOnSaveEnabled = false; // Added to avoid issue with confirm password match on save changes
                            de.SaveChanges();
                            Status = true;
                            message = "Reset password link has been sent to your email at: " + emailID;
                        }
                        else
                        {
                            message = "No account linked to email " + emailID + " was found!";
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
            ViewBag.Status = Status;
            return View();
        }
        #endregion

        #region // Reset Password Action
        [HttpGet]
        public ActionResult ResetPassword(string id)
        {
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var user = de.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
                if (user != null)
                {
                    ResetPasswordModel resetPasswordModel = new ResetPasswordModel();
                    resetPasswordModel.ResetCode = id;
                    return View(resetPasswordModel);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }
        #endregion

        #region // Reset Password POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel resetPasswordModel)
        {
            var message = "";
            if (ModelState.IsValid)
            {
                using (DatabaseEntities de = new DatabaseEntities())
                {
                    var user = de.Users.Where(a => a.ResetPasswordCode == resetPasswordModel.ResetCode).FirstOrDefault();
                    if (user != null)
                    {
                        user.Password = Crypto.Hash(resetPasswordModel.NewPassword);
                        user.ResetPasswordCode = "";
                        de.Configuration.ValidateOnSaveEnabled = false; // Added to avoid issue with confirm password match on save changes
                        de.SaveChanges();
                        message = "New password updated successfully!";
                    }
                }
            }
            else
            {
                message = "Invalid Request!";
            }

            ViewBag.Message = message;
            return View(resetPasswordModel);
        }
        #endregion

        #region // View Profile Action
        [Authorize]
        [HttpGet]
        public ActionResult ViewMyProfile()
        {
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var user = de.Users.Where(a => a.EmailID == HttpContext.User.Identity.Name).FirstOrDefault();
                if (user != null)
                {
                    ViewBag.ViewProfileUser = user;
                    return View(user);
                }
            }
            ViewBag.ViewProfileUser = null;
            return View();
        }
        #endregion

        #region // Edit Profile Action
        [Authorize]
        [HttpGet]
        public ActionResult EditProfile()
        {
            return View();
        }
        #endregion

        #region // Edit Profile POST Action
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProfile(UserEditModel userEditModel)
        {
            string message = "";
            bool Status = false;
            bool EmailChanged = false;
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var user = de.Users.Where(a => a.EmailID == HttpContext.User.Identity.Name).FirstOrDefault();
                if (user != null)
                {
                    if (userEditModel.FirstName != null)
                    {
                        if (!userEditModel.FirstName.Any(x => Char.IsWhiteSpace(x)))
                        {
                            user.FirstName = userEditModel.FirstName;
                            Status = true;
                        }
                    }

                    if (userEditModel.LastName != null)
                    {
                        if (!userEditModel.LastName.Any(x => Char.IsWhiteSpace(x)))
                        {
                            user.LastName = userEditModel.LastName;
                            Status = true;
                        }
                    }

                    if (userEditModel.EmailID != null)
                    {
                        if (!userEditModel.EmailID.Any(x => Char.IsWhiteSpace(x)))
                        {
                            if (IsEmail(userEditModel.EmailID))
                            {
                                var videos = de.Videos.Where(a => a.VideoAuthor == user.EmailID);
                                FormsAuthentication.SignOut();
                                foreach (var video in videos)
                                {
                                    video.VideoAuthor = userEditModel.EmailID;
                                }
                                user.EmailID = userEditModel.EmailID;
                                Status = true;
                                EmailChanged = true;
                                de.Configuration.ValidateOnSaveEnabled = false;
                                de.SaveChanges();
                                return RedirectToAction("DisplayAllVideos", "Video");
                            }
                        }
                    }

                    if (userEditModel.DateOfBirth != null)
                    {
                        user.DateOfBirth = userEditModel.DateOfBirth;
                        Status = true;
                    }

                    if (EmailChanged == false)
                    {
                        de.Configuration.ValidateOnSaveEnabled = false;
                        de.SaveChanges();
                    }

                    if (Status == true)
                    {
                        message = "Profile updated successfully!";
                    }
                    else
                    {
                        message = "Please input at least one value!";
                    }
                }
            }

            ViewBag.Message = message;
            return View(userEditModel);
        }
        #endregion

        #region // View User Profile Action
        [HttpGet]
        public ActionResult ViewUserProfile(string emailID)
        {
            string message = "";
            if ((emailID != "") && (emailID != null))
            {
                if (IsEmail(emailID))
                {
                    using (DatabaseEntities de = new DatabaseEntities())
                    {
                        var user = de.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                        if (user != null)
                        {
                            ViewBag.ViewProfileUser = user;
                            return View(user);
                        }
                        else
                        {
                            message = "No account linked to email " + emailID + " was found!";
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

            ViewBag.ViewProfileUser = null;
            ViewBag.Message = message;
            return View();
        }
        #endregion

        #region // View All Users Action
        [HttpGet]
        public ActionResult ViewAllUsers()
        {
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var users = de.Users.ToList();
                if (users != null)
                {
                    return View(users);
                }
            }
            return View();
        }
        #endregion

        #region // Helper Functions
        [NonAction]
        public bool DoesEmailExist(string emailID)
        {
            using (DatabaseEntities de = new DatabaseEntities())
            {
                var exists = de.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                return exists != null;
            }
        }

        [NonAction]
        public void SendVerificationEmail(string emailID, string activationCode, string emailPurpose = "VerifyAccount")
        {
            var verifyUrl = "/User/"+ emailPurpose + "/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("venomcarnage97@gmail.com", "Web Video Portal");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = ""; // Replace with actual password

            string subject = "";
            string body = "";

            if (emailPurpose == "VerifyAccount")
            {
                subject = "Your account is successfully created!";
                body = "<br/><br/>We are excited to tell you that your Web Video Portal account is successfully created. Please click on the link below to verify your account:<br/><br/><a href = '" + link + "'>" + link + "</a>";
            }
            else if (emailPurpose == "ResetPassword")
            {
                subject = "Reset Password";
                body = "Hello,<br/><br/>We got a request for resetting your password. Please click on the link below to reset your password:<br/><br/><a href = '"+ link + "'>Reset Password</a>";
            }

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })

            smtp.Send(message);
        }

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