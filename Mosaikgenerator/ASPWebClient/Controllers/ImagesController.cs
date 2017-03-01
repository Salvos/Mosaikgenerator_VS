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
using Contracts;

namespace ASPWebClient.Controllers
{
    [Authorize]
    public class ImagesController : Controller
    {
        private DBModelContainer db = new DBModelContainer();

        // DEBUGGING
        // Statischer Bilderpfad - Muss im nachhinein entfernt ODER "" gesetzt werden.
        private const string IMAGEPATH = "D:\\Bilder\\Projekte\\MosaikGenerator\\";

        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null)
            {
                return HttpNotFound();
            }

            return View(images);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null)
            {
                return HttpNotFound();
            }
            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PoolsId,path,filename,displayname,width,heigth,hsv")] Images images)
        {
            if (ModelState.IsValid)
            {
                db.Entry(images).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Pools", new { id = images.PoolsId });
            }
            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null)
            {
                return HttpNotFound();
            }
            return View(images);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Images images = db.ImagesSet.Find(id);
            Pools thisPool = db.PoolsSet.Find(images.PoolsId);
            if(!thisPool.writelock)
            {
                // Speichere die Änderungen in der Datenbank
                db.ImagesSet.Remove(images);
                db.SaveChanges();

                // Entferne das Bild im FileSystem
                System.IO.File.Delete(IMAGEPATH+images.path+images.filename);

                return RedirectToAction("Details", "Pools", new { id = images.PoolsId });
            }
            return RedirectToAction("Delete", "Pools", new { id = images.PoolsId });
        }

        public ActionResult Mosaik(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Basis = id;
            var poolsSet = db.PoolsSet.Where(p => p.owner == User.Identity.Name);

            return View(poolsSet.ToList());
        }

        [HttpPost, ActionName("Mosaik")]
        [ValidateAntiForgeryToken]
        public ActionResult GenMosaik(int? id, String kachelPool, String mosaPool, String bestof = "1", String multi = "0")
        {
            if (id == null && kachelPool == "" && mosaPool == "")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Basis = id;

            EndpointAddress endPoin = new EndpointAddress("http://localhost:8080/mosaikgenerator/mosaikgenerator");
            ChannelFactory<IMosaikGenerator> channelfactory = new ChannelFactory<IMosaikGenerator>(new BasicHttpBinding(), endPoin);
            IMosaikGenerator proxy = null;

            try
            {
                proxy = channelfactory.CreateChannel();

                proxy.mosaikGenerator((int)id, int.Parse(kachelPool), int.Parse(mosaPool), multi == "1", int.Parse(bestof));
            }
            catch (Exception)
            {
                channelfactory.Close();
            }

            channelfactory.Close();

            return RedirectToAction("Details", "Pools", new { id = mosaPool });
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
