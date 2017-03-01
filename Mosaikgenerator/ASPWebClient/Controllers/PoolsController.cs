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
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Bildersammlungen([Bind(Include = "Id,owner,name,size,writelock")] Pools pools)
        {
            if (ModelState.IsValid)
            {
                db.PoolsSet.Add(pools);
                db.SaveChanges();
            }

            return RedirectToAction("Bildersammlungen");
        }

        public ActionResult Kacheln()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Kacheln([Bind(Include = "Id,owner,name,size,writelock")] Pools pools)
        {
            if (ModelState.IsValid)
            {
                db.PoolsSet.Add(pools);
                db.SaveChanges();
            }

            return RedirectToAction("Kacheln");
        }

        public ActionResult PoolsList(bool? isKachelPool)
        {
            var user = User.Identity.Name;
            ViewBag.isKachelPool = isKachelPool;

            IQueryable<Pools> poolsSet;

            if ((bool)isKachelPool)
            {
                poolsSet = db.PoolsSet.Where(k => k.size != 0).Where(p => p.owner == user);
            }
            else
            {
                poolsSet = db.PoolsSet.Where(k => k.size == 0).Where(p => p.owner == user);
            }

            return PartialView(poolsSet.ToList());
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

            ViewBag.Poolname = pools.name;
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

            return View("Details", imagesSet);
        }

        [HttpPost, ActionName("Details")]
        [ValidateAntiForgeryToken]
        public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> files, int? id)
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

            String folder = "Kacheln";
            if (pools.size == 0)
                folder = "Motive";

            foreach (var file in files)
            {
                String UUID = Guid.NewGuid().ToString();
                String extention = System.IO.Path.GetExtension(file.FileName);
                string filename = UUID + extention;

                file.SaveAs("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + filename);

                Bitmap bmp = new Bitmap("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + filename);

                String dateiname = file.FileName;
                int fileExtPos = dateiname.LastIndexOf(".");
                if (fileExtPos >= 0)
                    dateiname = dateiname.Substring(0, fileExtPos);

                if (pools.size == 0)
                    db.Set<Motive>().Add(new Motive { path = folder + "\\", filename = filename, PoolsId = pools.Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0", readlock = false, writelock = false });
                else
                    db.Set<Kacheln>().Add(new Kacheln { path = folder + "\\", filename = filename, PoolsId = pools.Id, displayname = dateiname, heigth = bmp.Height, width = bmp.Width, hsv = "0" });//, avgR = (int)red, avgG = (int)green, avgB = (int)blue });

                db.SaveChanges();

                bmp.Dispose();

                // Croppen und Scalen wenn Bild eine Kachel ist
                if (pools.size > 0)
                {
                    EndpointAddress endPoint = new EndpointAddress("http://localhost:8080/mosaikgenerator/imagehandler");
                    ChannelFactory<IHandler> channelFactory = new ChannelFactory<IHandler>(new BasicHttpBinding(), endPoint);
                    IHandler proxy = null;

                    int imageId = db.ImagesSet.Where(p => p.filename == filename).First().Id;

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

        public ActionResult PoolCount(int id)
        {
            int imageCount = db.ImagesSet.Where(o => o.PoolsId == id).Count();
            return PartialView(imageCount);
        }

        public ActionResult Thumbnail(int image, bool isKachel)
        {
            var pic = db.ImagesSet.Where(i => i.Id == image).First();

            String folder = "Kacheln";
            if (!isKachel)
                folder = "Motive";

            byte[] byteImage = System.IO.File.ReadAllBytes("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + pic.filename);

            return File(byteImage, "image/png");
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
