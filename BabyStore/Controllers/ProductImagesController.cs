using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BabyStore.DAL;
using BabyStore.Models;
using System.Web.Helpers;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;

namespace BabyStore.Controllers
{
    public class ProductImagesController : Controller
    {
        private StoreContext db = new StoreContext();

        // GET: ProductImages
        public ActionResult Index()
        {
            return View(db.ProductImages.ToList());
        }

        // GET: ProductImages/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductImage productImage = db.ProductImages.Find(id);
            if (productImage == null)
            {
                return HttpNotFound();
            }
            return View(productImage);
        }

        // GET: ProductImages/Create
        public ActionResult Upload()
        {
            return View();
        }

        // POST: ProductImages/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase[] files)
        {
            bool allValid = true;
            string inValidFiles = "";
            db.Database.Log = sql => Trace.WriteLine(sql);

            //validation that user has entered a file before hitting button
            if (files[0] != null)
            {
                //check for less then 10 files, max number 
                if (files.Length <= 10)
                {
                    foreach (var file in files)
                    {
                        if (!ValidateFile(file))
                        {
                            allValid = false;
                            inValidFiles += ", " + file.FileName;
                        }
                    }
                    if (allValid)
                    {
                        foreach (var file in files)
                        {
                            try
                            {
                                SaveFileToDisk(file);
                            }
                            catch (Exception)
                            {

                                ModelState.AddModelError("FileName", "Error occurred saving the files to disk, try again");
                            }
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("FileName", "All files must be a gif, png, jpeg, jpg and less than 2MB in size. The following files" +
                            inValidFiles + " are not valid");
                    }
                }
           
                //or if he has entered more then 10 files **error handling
                else
                {
                    ModelState.AddModelError("FileName", "Please upload 10 files only at a time");
                }
            }
            else
            {
                //there is no files uploaded add error handling message/ changing modelstate
                ModelState.AddModelError("FileName", "Please choose a file");
            }
            if (ModelState.IsValid)
            {
                bool duplicates = false;
                bool otherDbError = false;
                string duplicateFiles = "";

                foreach (var file in files)
                {
                    var prodcutToAdd = new ProductImage { FileName = file.FileName };
                    try
                    {
                        db.ProductImages.Add(prodcutToAdd);
                        db.SaveChanges();
                    }
                    catch (DbUpdateException ex)
                    {
                        //set innerException to the DbUpdateException as SqlException (looking for duplicate index code error)
                        SqlException innerException = ex.InnerException.InnerException as SqlException;
                        //if there is an innerException and the code is 2601 (sql number for duplicate index)
                        if (innerException != null && innerException.Number == 2601)
                        {
                            //create record of file names that were duplicates
                            duplicateFiles += "," + file.FileName;
                            duplicates = true;
                            //detach the duplicate file from the enitiy context//ensuring that the duplicate error does not get replicated
                            //on all elements in the enitiy
                            db.Entry(prodcutToAdd).State = EntityState.Detached;
                        }
                        else
                        {
                            //if error was not due to duplicate files uploated pop flag to true
                            otherDbError = true;
                        }
                        
                    }
                }
                //checking flags and error handling on them
                if (duplicates)
                {
                    ModelState.AddModelError("FileName", "All files uploaded except the files" + duplicateFiles + ", which already exist in the system. " +
                        "please delete them and try again to add new files");
                    return View();
                }
                else if (otherDbError)
                {
                    ModelState.AddModelError("FileName", "Sorry an unknown Error has occurred during saving of the files, please adjust and try again");
                    return View();
                }
                return RedirectToAction("Index");
            }
            //return input view not redirect to index for viewing
            return View();
        }

        // GET: ProductImages/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductImage productImage = db.ProductImages.Find(id);
            if (productImage == null)
            {
                return HttpNotFound();
            }
            return View(productImage);
        }

        // POST: ProductImages/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,FileName")] ProductImage productImage)
        {
            if (ModelState.IsValid)
            {
                db.Entry(productImage).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(productImage);
        }

        // GET: ProductImages/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ProductImage productImage = db.ProductImages.Find(id);
            if (productImage == null)
            {
                return HttpNotFound();
            }
            return View(productImage);
        }

        // POST: ProductImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ProductImage productImage = db.ProductImages.Find(id);
            db.ProductImages.Remove(productImage);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        private bool ValidateFile(HttpPostedFileBase file)
        {
            string fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();
            string[] allowedFileTypes = { ".gif", ".png", ".jpeg", ".jpg" };
            if((file.ContentLength > 0 && file.ContentLength < 2097152) && allowedFileTypes.Contains(fileExtension))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void SaveFileToDisk(HttpPostedFileBase file)
        {
            WebImage img = new WebImage(file.InputStream);
            if (img.Width > 190)
            {
                img.Resize(190, img.Height);
            }
            img.Save(Constants.ProductImagePath + file.FileName);
            if (img.Width > 100)
            {
                img.Resize(100, img.Height);
            }
            img.Save(Constants.ProductThumbnailPath + file.FileName);
            
        }
    }
}
