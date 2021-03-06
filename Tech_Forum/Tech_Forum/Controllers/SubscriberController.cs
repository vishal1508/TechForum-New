﻿using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Tech_Forum.Models;
using System.IO;
using System.Web.Security;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using Newtonsoft.Json;
using Tech_Forum.ServiceReference;

namespace Tech_Forum.Controllers
{
    public class SubscriberController : Controller
    {
        //Registration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }

        //Registration POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")]Subscriber_Table subscriber)
        {
            bool Status = false;
            string message = "";
            StreamWriter stream = null;

            //Model Validation
            if (ModelState.IsValid)
            {
                #region Email is already existing
                var isExist = IsEmailExist(subscriber.email);
                if (isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email already exist");
                    return View(subscriber);
                }
                #endregion

                #region Activation Code Generation
                subscriber.ActivationCode = Guid.NewGuid();
                #endregion

                #region Password Hashing
                subscriber.password = Crypto.Hash(subscriber.password);
                subscriber.confirmpassword = Crypto.Hash(subscriber.confirmpassword);
                #endregion
                subscriber.IsEmailVerified = false;

                #region Save data to database
                using (DbAccessEntity db = new DbAccessEntity())
                {
                    db.Subscriber_Table.Add(subscriber);
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (DbEntityValidationException dbEx)
                    {
                        stream= new StreamWriter(@"D:\Exception.txt");
                        foreach (var validationErrors in dbEx.EntityValidationErrors)
                        {
                            foreach (var validationError in validationErrors.ValidationErrors)
                            {
                                stream.WriteLine("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                            }
                        }
                        stream.Close();
                    }
                    finally
                    {
                        //stream.Close();
                    }


                    //Send Email to user
                    SendVerificationLinkEmail(subscriber.email, subscriber.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link has been sent to your emailid " + subscriber.email;

                    Status = true;

                }
                #endregion

            }
            else
            {
                message = "Invalid request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;


            return View(subscriber);
        }

        //Verify Account
        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using (DbAccessEntity db = new DbAccessEntity())
            {
                db.Configuration.ValidateOnSaveEnabled = false; //This line is added to avoid 
                                                                //confirm password does not match issue
                var v = db.Subscriber_Table.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();

                if (v != null)
                {
                    v.IsEmailVerified = true;
                    db.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
            ViewBag.Status = Status;
            return View();
        }


        //Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        //Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(SubscriberLogin login,string ReturnUrl)
        {
            string message = "";
            using (DbAccessEntity db = new DbAccessEntity())
            {
                var v = db.Subscriber_Table.Where(a => a.email == login.email).FirstOrDefault();
                if(v != null)
                {
                    if (string.Compare(Crypto.Hash(login.password),v.password)==0)
                    {
                        int timeout = login.RememberMe ? 525600 : 20; //525600 minutes equals 1 year
                        var ticket = new FormsAuthenticationTicket(v.name, login.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);
                        Session["userid"] = v.userid;
                        Session.Timeout = timeout;

                        if (Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("ManageUser");
                        }
                    }
                    else
                    {
                        message = "Invalid credentials provided";
                    }
                }
                else
                {
                    message = "Invalid credentials provided";
                }

            }

            ViewBag.Message = message;
            return View(login);
        }

        //Logout
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Post");
        }

        //Manage User action
        [Authorize]
        public ActionResult ManageUser()
        {
            using (DbAccessEntity db = new DbAccessEntity())
            {
                string userid = Session["userid"].ToString();
                List<Post_Table> article = db.Post_Table.Where(x => x.userid.Equals(userid) && x.category == true).ToList();
                List<Post_Table> blog = db.Post_Table.Where(x => x.userid.Equals(userid) && x.category == false).ToList();

                if (article.Count > 0)
                {
                    ViewData["Articles"] = article;
                }
                else
                {
                    ViewData["Articles"] = null;
                    ViewBag.ArticleMessage = "No articles published ";
                }

                if (blog.Count > 0)
                {
                    ViewData["Blogs"] = blog;
                }
                else
                {
                    ViewData["Blogs"] = null;
                    ViewBag.BlogMessage = "No blogs posted ";
                }
                
                

                return View();
            }
               
        }


        // GET: Subscriber/EditPost/5
        [Authorize]
        public ActionResult EditPost(int id)
        {

            using (DbAccessEntity db = new DbAccessEntity())
            {   
                List<Domain_Table> DomainList = db.Domain_Table.ToList();
                ViewBag.DomainList = new SelectList(DomainList, "did", "domain");

                List<Technology_Table> TechnologyList = db.Technology_Table.ToList();
                ViewBag.TechnologyList = new SelectList(TechnologyList, "tid", "technology");

                Post_Table post = db.Post_Table.Where(x => x.postid == id).FirstOrDefault();
                return View(post);
            }
        }

        // POST: Subscriber/EditPost/5
        [HttpPost]
        [Authorize]
        public ActionResult EditPost(int id, Post_Table post)
        {
            StreamWriter stream = null;
            
            try
            {
                using (DbAccessEntity db = new DbAccessEntity())
                {
                    List<Domain_Table> DomainList = db.Domain_Table.ToList();
                    ViewBag.DomainList = new SelectList(DomainList, "did", "domain");

                    db.Configuration.ProxyCreationEnabled = false;

                    List<Technology_Table> TechnologyList = db.Technology_Table.ToList();
                    ViewBag.TechnologyList = new SelectList(TechnologyList, "tid", "technology");

                    //Get all the values of article/blog
                    var postvalues = db.Post_Table.Where(x => x.postid == id).FirstOrDefault();

                    //set all the values
                    post.postid = postvalues.postid;
                    post.date = postvalues.date;
                    post.category = postvalues.category;
                    post.userid = postvalues.userid;

                    int did = Convert.ToInt32(post.domain);
                    int tid = Convert.ToInt32(post.technology);
                    var d = db.Domain_Table.Where(x => x.did == did).FirstOrDefault();
                    post.domain = d.domain;
                    var t = db.Technology_Table.Where(x => x.tid == tid).FirstOrDefault();
                    post.technology = t.technology;
                    post.userid = Session["userid"].ToString();
                    ViewData["Article"] = post;
                    db.Post_Table.AddOrUpdate(post);
                    db.SaveChanges();
          
                }
                return View("../Post/ResultView");
            }
            catch(Exception e)
            {
                stream = new StreamWriter(@"D:/EditException.txt");
                stream.WriteLine(e);
                stream.Close();
                return View();
            }
        }

        //GET : Delete Post
        [Authorize]
        public ActionResult DeletePost(int id)
        {
            using (DbAccessEntity db = new DbAccessEntity())
            {
                return View(db.Post_Table.Where(x => x.postid == id).FirstOrDefault());
            }
        }

        //POST : Delete Post
        [HttpPost]
        [Authorize]
        public ActionResult DeletePost(int id,FormCollection form)
        {
            try
            {
                using (DbAccessEntity db = new DbAccessEntity())
                {
                    Post_Table post = db.Post_Table.Where(x => x.postid == id).FirstOrDefault();
                    db.Post_Table.Remove(post);
                    db.SaveChanges();
                }

                return RedirectToAction("ManageUser");
            }
            catch
            {
                return View();
            }
        }



        //Details
        [Authorize]
        public ActionResult Details(int id,Article article,Rate rate)
        {
            using (DbAccessEntity db = new DbAccessEntity())
            {
                Post_Table post = db.Post_Table.Where(x => x.postid == id).FirstOrDefault();
                if (post.comments != null)
                {
                    article.comments = JsonConvert.DeserializeObject<List<Comment>>(post.comments);
                }

                Rate.rateList.Clear();

                if (post.rating != null)
                {
                    Rate.rateList = JsonConvert.DeserializeObject<List<Rate>>(post.rating);
                    rate.rating = Rate.getUserRating(Rate.rateList, Session["userid"].ToString());
                    if (rate.rating != 0)
                    {
                        rate.userId = Session["userid"].ToString();
                    }
                }

                ViewData["Article"] = post;
                ViewData["Comments"] = article;
                ViewData["Rate"] = rate;
                ViewData["AverageRating"] = Rate.calculateAverageRating(Rate.rateList);
                return View("../Post/ResultView");
            }
             
        }

        [NonAction]
        public bool IsEmailExist(string emailid)
        {
            using (DbAccessEntity db = new DbAccessEntity())
            {
                var v = db.Subscriber_Table.Where(a => a.email == emailid).FirstOrDefault();
                return v != null;
            }
        }

        [NonAction]
        public void SendVerificationLinkEmail(string emailid,string activationcode,string emailFor="VerifyAccount")
        {
            var verifyUrl = "/Subscriber/"+emailFor+"/" + activationcode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("techforumofcl@gmail.com", "Tech Forum");
            var toEmail = new MailAddress(emailid);
            var fromEmailPassword = "sagamaga@123";


            string subject = "";
            string body = "";
            if (emailFor=="VerifyAccount")
            {
                subject = "Your account is successfully created !";

                body = "<br/><br/> We are excited to tell you that your Tech Forum account is "
                    + " Successfully created. Please click on the below link to verify your account "
                    + " <br/><br/><a href='" + link + "'>" + link + "</a>";
            }
            else if(emailFor == "ResetPassword")
            {
                subject = "Reset Password";

                body = "Hi,<br/><br/>We got request to reset your account password. Please click the below link to reset your password"+
                    "<br/><br/><a href="+link+">Reset Password Link</a>";
            }
            
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address,fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true

            })

                try
                {
                    smtp.Send(message);
                }
                catch(Exception e)
                {
                    Response.Redirect("www.google.com");
                }
            
        }

        // Forgot Password

        public ActionResult ForgotPassword()
        {
            return View();
        }

        //Verify the forgot password
        [HttpPost]

        public ActionResult ForgotPassword(string EmailID)
        {
            //Verify the emailid
            //Generate Reset Password Link
            //Send Email
            string message = "";
            bool status = false;

            using (DbAccessEntity db = new DbAccessEntity())
            {
                var account = db.Subscriber_Table.Where(x => x.email == EmailID).FirstOrDefault();
                if (account != null)
                {
                    //Send email for reset password
                    string resetCode = Guid.NewGuid().ToString();
                    SendVerificationLinkEmail(account.email, resetCode, "ResetPassword");
                    account.ResetPasswordCode = resetCode;

                    db.Configuration.ValidateOnSaveEnabled = false;
                    db.SaveChanges();
                    message = "Reset Password Link has been sent to your Email ID";
                }
                else
                {
                    message = "Account Not Found";
                }
            }
            ViewBag.Message = message;
            return View();
        }

        public ActionResult ResetPassword(string id)
        {
            //Verify the reset password link
            //Find accound associated with this link
            //redirect to reset password page
            using (DbAccessEntity db = new DbAccessEntity())
            {
                var user = db.Subscriber_Table.Where(x => x.ResetPasswordCode == id).FirstOrDefault();
                if (user != null)
                {
                    ResetPasswordModel model = new ResetPasswordModel();
                    model.ResetCode = id;
                    return View(model);
                }
                else
                {
                    return HttpNotFound();
                }
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModel model)
        {
            var message = "";
            if (ModelState.IsValid)
            {
                using (DbAccessEntity db = new DbAccessEntity())
                {
                    var user = db.Subscriber_Table.Where(x => x.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                    if (user != null)
                    {
                        user.password = Crypto.Hash(model.NewPassword);
                        user.ResetPasswordCode = "";
                        db.Configuration.ValidateOnSaveEnabled = false;
                        db.SaveChanges();
                        message = "New password updated successfully";
                    }
                }
            }
            else
            {
                message = "Something invalid";
            }

            ViewBag.Message = message;
            return View(model);
        }
    }
}