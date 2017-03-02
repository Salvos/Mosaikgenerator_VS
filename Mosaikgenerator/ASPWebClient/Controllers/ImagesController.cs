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
        /*=====Konstanten=====*/
        /// <summary>
        /// "Datenbankverbindung"
        /// </summary>
        private DBModelContainer db = new DBModelContainer();

        // Statischer Bilderpfad
        private static string IMAGEPATH = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\VS16_MosaikGenerator\\";

        /// <summary>
        /// Beim Aufruf der Index (/Images/) soll der User auf die Startseite umgeleitet werden
        /// </summary>
        /// <returns>Redirect</returns>
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Gibt die Informationen eines Bildes an die View zurück
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">ID des Bildes</param>
        /// <returns>BadRequest / NotFound / View</returns>
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            return View(images);
        }

        /// <summary>
        /// Editieren der Anzeigenamen der Bilder (mehr nicht!)
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">ID des Bildes</param>
        /// <returns>BadRequest / NotFound / View</returns>
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }
            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        /// <summary>
        /// Post Request
        /// Editieren der Anzeigenamen der Bilder (mehr nicht!)
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="images">ImageObjekt welches verändert wurde</param>
        /// <returns>NotFound / Redirect / View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PoolsId,path,filename,displayname,width,heigth,hsv")] Images images)
        {
            Images img = db.ImagesSet.Find(images.Id);
            if (images == null || img.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            if (ModelState.IsValid)
            {
                db.Entry(images).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Details", "Pools", new { id = images.PoolsId });
            }

            ViewBag.PoolsId = new SelectList(db.PoolsSet, "Id", "name", images.PoolsId);
            return View(images);
        }

        /// <summary>
        /// Löschen eines Bildes aus der Datenbank & dem Filesystem
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">ID des Bildes</param>
        /// <returns>BadRequest / NotFound / View</returns>
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }
            return View(images);
        }

        /// <summary>
        /// Löschen eines Bildes aus der Datenbank & dem Filesystem
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// Die Funktion wird erst wirksam wenn Writelocks gesetzt sind
        /// </summary>
        /// <param name="id">ID des Bildes</param>
        /// <returns>NotFound / View</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            Pools thisPool = db.PoolsSet.Find(images.PoolsId);
            bool writelock = false;

            // Wenn der Pool ein MotivPool ist (und das Bild ein Motiv ist) gibt es einen spezifischen Writelock
            if (thisPool.size == 0)
                writelock = db.Set<Motive>().Find(id).writelock;

            if (!thisPool.writelock && !writelock)
            {
                // Speichere die Änderungen in der Datenbank
                db.ImagesSet.Remove(images);
                db.SaveChanges();

                // Entferne das Bild im FileSystem
                System.IO.File.Delete(IMAGEPATH + images.path + images.filename);

                return RedirectToAction("Details", "Pools", new { id = images.PoolsId });
            }
            return RedirectToAction("Delete", "Pools", new { id = images.PoolsId });
        }

        /// <summary>
        /// Erstellen eines Mosaikbildes
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">ID des Bildes</param>
        /// <returns>BadRequest / NotFound / View</returns>
        public ActionResult Mosaik(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            var poolsSet = db.PoolsSet.Where(p => p.owner == User.Identity.Name);

            return View(poolsSet.ToList());
        }

        /// <summary>
        /// Erstellen eines Mosaikbildes
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">Id des Bildes</param>
        /// <param name="kachelPool">Id des Kachelpools</param>
        /// <param name="mosaPool">Id der Speichersammlung</param>
        /// <param name="bestof">Auswahl aus wievielen Bildern</param>
        /// <param name="multi">Kacheln mehrfach verwenden?</param>
        /// <returns>BadRequest</returns>
        [HttpPost, ActionName("Mosaik")]
        [ValidateAntiForgeryToken]
        public ActionResult GenMosaik(int? id, String kachelPool, String mosaPool, String bestof = "1", String multi = "0")
        {
            if (id == null && kachelPool == "" && mosaPool == "") {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Images images = db.ImagesSet.Find(id);
            if (images == null || images.Pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

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

        /// <summary>
        /// Erstellen eines Mosaikbildes
        /// Wurde das Bild nicht gefunden / ist nicht das eigene wird ein 404 ausgegeben
        /// </summary>
        /// <param name="id">Id des Bildes</param>
        /// <param name="kachelPool">Id des Kachelpools</param>
        /// <param name="mosaPool">Id der Speichersammlung</param>
        /// <param name="bestof">Auswahl aus wievielen Bildern</param>
        /// <param name="multi">Kacheln mehrfach verwenden?</param>
        /// <returns>BadRequest</returns>

        public ActionResult Download(int? id, bool isKachel)
        {
            Images image = db.ImagesSet.Where(p => p.Id == id).First();

            String folder = "Kacheln";
            if (!isKachel)
                folder = "Motive";

            byte[] fileBytes = System.IO.File.ReadAllBytes("D:\\Bilder\\Projekte\\MosaikGenerator\\" + folder + "\\" + image.filename);

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, image.filename);
        }

        /// <summary>
        /// Schon vorhandene Funktion zum "entsorgen" der Datenbankverbindung
        /// </summary>
        /// <param name="disposing">Trennen der Datenbankverbindung</param>
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
