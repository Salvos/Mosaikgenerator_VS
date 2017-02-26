using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Datenbank.DAL;
using System.Drawing;

namespace WebClient.Controllers
{
    public class PoolsController : Controller
    {
        private DBModelContainer db = new DBModelContainer();

        public ActionResult Index()
        {
            //var poolsSet = db.PoolsSet.Include(p => p.User);
            //return View(poolsSet.ToList());
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Bildersammlungen()
        {
            var poolsSet = db.PoolsSet.Where(k => k.size == 0);
            return View(poolsSet.ToList());
        }

        public ActionResult Kacheln()
        {
            var poolsSet = db.PoolsSet.Where(k => k.size != 0);
            return View(poolsSet.ToList());
        }

        public PartialViewResult CreatePool(bool? isKachelPool)
        {
            if (isKachelPool == null)
            {
                isKachelPool = false;
            }

            ViewBag.isKachelPool = isKachelPool;
            ViewBag.added = false;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public PartialViewResult CreatePool([Bind(Include = "Id,UserId,name,size,writelock")] Pools pools, bool? isKachelPool)
        {
            ViewBag.added = false;
            if (isKachelPool == null)
            {
                isKachelPool = false;
            }

            ViewBag.isKachelPool = isKachelPool;
            if (ModelState.IsValid)
            {
                db.PoolsSet.Add(pools);
                db.SaveChanges();
                ViewBag.added = true;
                return PartialView();
            }
            return PartialView();
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null)
            {
                return HttpNotFound();
            }
            ViewBag.added = false;
            var imagesSet = db.ImagesSet.Include(p => p.Pools).Where(k => k.PoolsId == pools.Id);
            ViewBag.Poolname = pools.name;
            ViewBag.isKachel = pools.size > 0;

            return View(imagesSet.ToList());
        }

        [HttpPost, ActionName("Details")]
        [ValidateAntiForgeryToken]
        public ActionResult UploadImage(HttpPostedFileBase file, int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Pools pools = db.PoolsSet.Find(id);

            if (pools == null)
            {
                return HttpNotFound();
            }

            ViewBag.Poolname = pools.name;

            if (file != null)
            {
                String folder = "Kacheln";
                if (pools.size == 0)
                    folder = "Basismotive";

                file.SaveAs("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + file.FileName);

                Bitmap bmp = new Bitmap("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + file.FileName);

                String dateiname = file.FileName;
                int fileExtPos = dateiname.LastIndexOf(".");
                if (fileExtPos >= 0)
                    dateiname = dateiname.Substring(0, fileExtPos);

                double red = 0;
                double green = 0;
                double blue = 0;
                var ges = bmp.Width * bmp.Height;
                for (int i = 0; i < bmp.Width; i++)
                {
                    for (int j = 0; j < bmp.Height; j++)
                    {
                        Color rgb = bmp.GetPixel(i, j);
                        red += rgb.R;
                        green += rgb.G;
                        blue += rgb.B;
                    }
                }
                red = red / ges;
                green = green / ges;
                blue = blue / ges;

                if (pools.size == 0)
                    db.Set<Motive>().Add(new Motive { path = folder + "\\", filename = file.FileName, PoolsId = db.PoolsSet.Where(p => p.owner == "Demo" && p.name == folder).First().Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0", readlock = false, writelock = false });
                else
                    db.Set<Kacheln>().Add(new Kacheln { path = folder + "\\", filename = file.FileName, PoolsId = db.PoolsSet.Where(p => p.owner == "Demo" && p.name == folder).First().Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0", avgR = (int)red, avgG = (int)green, avgB = (int)blue });

                db.SaveChanges();
            }

            var imagesSet = db.ImagesSet.Include(p => p.Pools).Where(k => k.PoolsId == pools.Id);

            return View(imagesSet.ToList());
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null)
            {
                return HttpNotFound();
            }

            ViewBag.Poolname = pools.name;
            ViewBag.isKachelPool = pools.size != 0 ? true : false;
            return View(pools);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,UserId,name,size,writelock")] Pools pools)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pools).State = EntityState.Modified;
                db.SaveChanges();
                if (pools.size == 0)
                    return RedirectToAction("Bildersammlungen");
                return RedirectToAction("Kacheln");
            }
            return View(pools);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null)
            {
                return HttpNotFound();
            }
            ViewBag.Poolname = pools.name;
            ViewBag.isKachelPool = pools.size != 0 ? true : false;
            return View(pools);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Pools pools = db.PoolsSet.Find(id);
            db.PoolsSet.Remove(pools);
            db.SaveChanges();
            if (pools.size == 0)
                return RedirectToAction("Bildersammlungen");
            return RedirectToAction("Kacheln");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
