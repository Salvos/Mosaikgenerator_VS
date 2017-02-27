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

namespace WebClient.Controllers
{
    public class ImagesController : Controller
    {
        private DBModelContainer db = new DBModelContainer();

        public ActionResult Index()
        {
            //var imagesSet = db.ImagesSet.Include(i => i.Pools);
            //return View(imagesSet.ToList());
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

        public ActionResult Mosaik(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Basis = id;
            ViewBag.Test = "test";

            var poolsSet = db.PoolsSet;

            return View(poolsSet.ToList());
        }

        // POST: Images/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Images images = db.ImagesSet.Find(id);
            db.ImagesSet.Remove(images);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [HttpPost, ActionName("Mosaik")]
        [ValidateAntiForgeryToken]
        public ActionResult GenMosaik(int? id, String kachelPool, String mosaPool, String bestof = "1", String multi = "0")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ViewBag.Test = "Mosaik";
            ViewBag.Basis = id;
            
            EndpointAddress endPoin = new EndpointAddress("http://localhost:8080/mosaikgenerator/mosaikgenerator");
            ChannelFactory<IMosaikGenerator> channelfactory = new ChannelFactory<IMosaikGenerator>(new BasicHttpBinding(), endPoin);
            IMosaikGenerator proxy = null;

            try
            {
                proxy = channelfactory.CreateChannel();

                proxy.mosaikGenerator((int)id, int.Parse(kachelPool), int.Parse(mosaPool), multi == "1", int.Parse(bestof));
            }
            catch(Exception)
            {
                channelfactory.Close();
            }

            channelfactory.Close();

            return Redirect("/Pools/Details/" + mosaPool);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }





        /*



        // GET: Images/Details/5
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

        // GET: Images/Create
        public ActionResult Create()
        {
            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name");
            return View();
        }

        // POST: Images/Create
        // Aktivieren Sie zum Schutz vor übermäßigem Senden von Angriffen die spezifischen Eigenschaften, mit denen eine Bindung erfolgen soll. Weitere Informationen 
        // finden Sie unter http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,PoolsId,path,filename,displayname,width,heigth,hsv")] Images images)
        {
            if (ModelState.IsValid)
            {
                db.ImagesSet.Add(images);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        // GET: Images/Edit/5
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

        // POST: Images/Edit/5
        // Aktivieren Sie zum Schutz vor übermäßigem Senden von Angriffen die spezifischen Eigenschaften, mit denen eine Bindung erfolgen soll. Weitere Informationen 
        // finden Sie unter http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PoolsId,path,filename,displayname,width,heigth,hsv")] Images images)
        {
            if (ModelState.IsValid)
            {
                db.Entry(images).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        // GET: Images/Delete/5
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

        // POST: Images/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Images images = db.ImagesSet.Find(id);
            db.ImagesSet.Remove(images);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

    */
    }
}
