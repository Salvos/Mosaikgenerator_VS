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
        /// <summary>
        /// "Datenbankverbindung"
        /// </summary>
        private DBModelContainer db = new DBModelContainer();

        /// <summary>
        /// Statischer Bilderpfad - verweist auf den "Meine Bilder" Ordner des Users
        /// </summary>
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
        /// Gibt genauso wie die Kacheln die Pool die Poolansicht zurück
        /// Das wurde gemacht damit die Kacheln und die Bildersammlungen auch optisch (vom Link) getrennt sind!
        /// </summary>
        /// <returns>View</returns>
        public ActionResult Bildersammlungen()
        {
            return View();
        }
 
        /// <summary>
        /// HTTP-POST
        /// Erstellt einen neuen Pool für den Benutzer in der jeweiligen Ansicht
        /// Das wurde gemacht damit die Kacheln und die Bildersammlungen auch optisch (vom Link) getrennt sind!
        /// </summary>
        /// <returns>Redirect</returns>
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

        /// <summary>
        /// Gibt genauso wie die Bildersammlungen die Pool die Poolansicht zurück
        /// Das wurde gemacht damit die Kacheln und die Bildersammlungen auch optisch (vom Link) getrennt sind!
        /// </summary>
        /// <returns>View</returns>
        public ActionResult Kacheln()
        {
            return View();
        }

        /// <summary>
        /// HTTP-POST
        /// Erstellt einen neuen Pool für den Benutzer in der jeweiligen Ansicht
        /// Das wurde gemacht damit die Kacheln und die Bildersammlungen auch optisch (vom Link) getrennt sind!
        /// </summary>
        /// <returns>Redirect</returns>
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

        /// <summary>
        /// Gibt die Liste mit allen Pools (je nach Kachel oder Bildersammlung) zurück
        /// </summary>
        /// <param name="isKachelPool">Ob der Pool ein Kachelpool ist</param>
        /// <returns>PartialView</returns>
        public ActionResult PoolsList(bool? isKachelPool)
        {
            ViewBag.isKachelPool = isKachelPool;

            IQueryable<Pools> poolsSet;

            if ((bool)isKachelPool)
            {
                poolsSet = db.PoolsSet.Where(k => k.size != 0).Where(p => p.owner == User.Identity.Name);
            }
            else
            {
                poolsSet = db.PoolsSet.Where(k => k.size == 0).Where(p => p.owner == User.Identity.Name);
            }

            return PartialView(poolsSet.ToList());
        }

        /// <summary>
        /// Gibt die Informationen zu dem Pool aus
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>View</returns>
        public ActionResult Details(int? id, bool? gen = null)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null || pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            ViewBag.generation = 0;

            if (gen != null)
            {
                ViewBag.generation = gen == false ? 1 : 2;
            }

            var imagesSet = db.ImagesSet.Include(p => p.Pools).Where(k => k.PoolsId == pools.Id);
            ViewBag.Poolname = pools.name;
            ViewBag.isKachel = pools.size > 0;
            ViewBag.id = id;

            return View(imagesSet.ToList());
        }

        /// <summary>
        /// HTTP-POST
        /// Schickt die Informationen zum generieren der Kacheln an den KachelGenerator
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <param name="color">Die Farbe mit der die Kacheln generiert werden sollen</param>
        /// <param name="count">Die Anzahl der zu generierenden Bilder</param>
        /// <param name="noise">Ob ein Rauschen eingefügt werden soll</param>
        /// <returns>BadRequest, HttpNotFound, View</returns>
        [HttpPost]
        public ActionResult GenKacheln(int? id, string color, string count, string noise = "0")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Pools pools = db.PoolsSet.Find(id);

            if (pools == null || pools.owner != User.Identity.Name)
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
                    proxy.genKachel(pools.Id, newColor.R, newColor.G, newColor.B, noise == "1");
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

        /// <summary>
        /// HTTP-POST
        /// Zum hochladen eines oder mehrerer Bilder 
        /// Wenn der Pool ein Kachelpool ist wird das Bild zudem zugeschnitten (cropping & scaling)
        /// </summary>
        /// <param name="files">Die Dateien die hochgeladen werden sollen</param>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>BadRequest, HttpNotFound, View</returns>
        [HttpPost, ActionName("Details")]
        [ValidateAntiForgeryToken]
        public ActionResult UploadImage(IEnumerable<HttpPostedFileBase> files, int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Pools pools = db.PoolsSet.Find(id);

            if (pools == null || pools.owner != User.Identity.Name)
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

                file.SaveAs(IMAGEPATH + folder + "\\" + filename);

                Bitmap bmp = new Bitmap(IMAGEPATH + folder + "\\" + filename);

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

        /// <summary>
        /// Gibt die View zum Editieren des Poolnamens zurück
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>BadRequest, HttpNotFound, View</returns>
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null || pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            ViewBag.Poolname = pools.name;
            ViewBag.isKachelPool = pools.size != 0 ? true : false;
            return View(pools);
        }

        /// <summary>
        /// HTTP-Post
        /// Gibt die View zum Editieren des Poolnamens zurück
        /// </summary>
        /// <param name="pools">Der Pool der bearbeitet wurde</param>
        /// <returns>Redirect, View</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,owner,name,size,writelock")] Pools pools)
        {
            Pools pool = db.PoolsSet.Find(pools.Id);
            if (pool == null || pool.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

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

        /// <summary>
        /// Gibt die View zum Löschen des Pools zurück
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>BadRequest, HttpNotFound, View</returns>
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null || pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }
            ViewBag.Poolname = pools.name;
            ViewBag.isKachelPool = pools.size != 0 ? true : false;
            return View(pools);
        }

        /// <summary>
        /// HTTP-POST
        /// Löscht einen angegebenen Pool und wechselt zur Poolübersicht
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>Redirect</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Pools pools = db.PoolsSet.Find(id);
            if (pools == null || pools.owner != User.Identity.Name)
            {
                return HttpNotFound();
            }

            String redirect = "Kacheln";
            if (pools.size == 0)
                redirect = "Bildersammlungen";

            bool writelock = false;
            if(pools.size == 0)
            {
                List<Motive> motive = db.ImagesSet.OfType<Motive>().Where(k => k.PoolsId == id).ToList();
                foreach(Motive motiv in motive)
                {
                    if (motiv.writelock)
                        writelock = true;
                    break;
                }
            }

            if (!pools.writelock && !writelock)
            {
                bool error = false;

                List<Images> images = db.ImagesSet.Where(k => k.PoolsId == id).ToList();
                try
                {
                    foreach (Images image in images)
                    {
                        // Entferne das Bild im FileSystem
                        System.IO.File.Delete(IMAGEPATH + image.path + image.filename);

                        // Speichere die Änderungen in der Datenbank
                        db.ImagesSet.Remove(image);
                        db.SaveChanges();
                    }
                } catch(Exception e)
                {
                    error = true;
                }

                if( !error )
                {
                    db.PoolsSet.Remove(pools);
                    db.SaveChanges();
                }

                return RedirectToAction(redirect);
            }

            return RedirectToAction(redirect);
        }

        /// <summary>
        /// Gibt die Anzahl der Bilder im Pool zurück
        /// </summary>
        /// <param name="id">Die ID des Pools</param>
        /// <returns>PartialView</returns>
        public ActionResult PoolCount(int id)
        {
            int imageCount = db.ImagesSet.Where(o => o.PoolsId == id && o.Pools.owner == User.Identity.Name).Count();
            return PartialView(imageCount);
        }

        /// <summary>
        /// Gibt das Bild aus dem Filesystem via Base64 zurück
        /// </summary>
        /// <param name="image">ID des Bildes</param>
        /// <param name="isKachel">Ob das Bild aus einem Kachelpool stammt</param>
        /// <returns>File</returns>
        public ActionResult Thumbnail(int image, bool isKachel)
        {
            var pic = db.ImagesSet.Where(i => i.Id == image && i.Pools.owner == User.Identity.Name).First();

            if(pic ==null)
            {
                return null;
            }

            String folder = "Kacheln";
            if (!isKachel)
                folder = "Motive";

            byte[] byteImage = System.IO.File.ReadAllBytes(IMAGEPATH + folder + "\\" + pic.filename);

            return File(byteImage, "image/png");
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
