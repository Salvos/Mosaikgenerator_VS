using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Datenbank.DAL;
using System.ServiceModel;
using System.Drawing;
using Contracts;

namespace ASPWebClient.Controllers
{
    [Authorize]
    public class PoolsController : Controller
    {
        private DBModelContainer db = new DBModelContainer();

        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Bildersammlungen()
        {
            var user = User.Identity.Name;
            var poolsSet = db.PoolsSet.Where(k => k.size == 0).Where(p => p.owner == user);
            return View(poolsSet.ToList());
        }

        public ActionResult Kacheln()
        {
            var user = User.Identity.Name;
            var poolsSet = db.PoolsSet.Where(k => k.size != 0).Where(p => p.owner == user);
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
        public PartialViewResult CreatePool([Bind(Include = "Id,owner,name,size,writelock")] Pools pools, bool? isKachelPool)
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
            ViewBag.id = id;

            return View(imagesSet.ToList());
        }

        [HttpPost]
        public ActionResult GenKacheln(int? id, string color, string count, string nois = "0")
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

            Color newColor = ColorTranslator.FromHtml(color);

            ViewBag.Poolname = newColor.R;//pools.name;
            ViewBag.isKachel = pools.size > 0;
            ViewBag.id = id;

            EndpointAddress endPoint = new EndpointAddress("http://localhost:8080/mosaikgenerator/kachelgenerator");
            ChannelFactory<IKachelGenerator> channelFactory = new ChannelFactory<IKachelGenerator>(new BasicHttpBinding(), endPoint);
            IKachelGenerator proxy = null;

            try
            {
                proxy = channelFactory.CreateChannel();
                for (int i = 0; i < int.Parse(count); i++)
                {
                    proxy.genKachel(pools.Id, newColor.R, newColor.G, newColor.B, nois == "1");
                }
            }
            catch (Exception e)
            {
                channelFactory.Close();
                Console.WriteLine(e.ToString());
            }

            channelFactory.Close();

            var imagesSet = db.ImagesSet.Include(p => p.Pools).Where(k => k.PoolsId == pools.Id);

            return View("Details", imagesSet.ToList());
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
            ViewBag.isKachel = pools.size > 0;
            ViewBag.id = id;

            if (file != null)
            {
                String folder = "Kacheln";
                if (pools.size == 0)
                    folder = "Motive";

                file.SaveAs("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + file.FileName);

                Bitmap bmp = new Bitmap("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + file.FileName);

                String dateiname = file.FileName;
                int fileExtPos = dateiname.LastIndexOf(".");
                if (fileExtPos >= 0)
                    dateiname = dateiname.Substring(0, fileExtPos);

                if (pools.size == 0)
                    db.Set<Motive>().Add(new Motive { path = folder + "\\", filename = file.FileName, PoolsId = pools.Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0", readlock = false, writelock = false });
                else
                    db.Set<Kacheln>().Add(new Kacheln { path = folder + "\\", filename = file.FileName, PoolsId = pools.Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0" });//, avgR = (int)red, avgG = (int)green, avgB = (int)blue });

                db.SaveChanges();

                bmp.Dispose();

                // Croppen und Scalen wenn Bild eine Kachel ist
                if (pools.size > 0)
                {
                    EndpointAddress endPoint = new EndpointAddress("http://localhost:8080/mosaikgenerator/imagehandler");
                    ChannelFactory<IHandler> channelFactory = new ChannelFactory<IHandler>(new BasicHttpBinding(), endPoint);
                    IHandler proxy = null;

                    int imageId = db.ImagesSet.Where(p => p.filename == file.FileName).First().Id;

                    try
                    {
                        proxy = channelFactory.CreateChannel();
                        proxy.cropRect(imageId);
                        proxy.scale(imageId, pools.size);
                    }
                    catch (Exception e)
                    {
                        channelFactory.Close();
                        Console.WriteLine(e.ToString());
                    }

                    channelFactory.Close();
                }
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
