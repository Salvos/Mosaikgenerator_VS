using Contracts;
using Datenbank.DAL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageHandler
{
    class Handler : IHandler
    {
        // "Datenbankverbindung"
        private static DBModelContainer db = new DBModelContainer();

        // Statischer Bilderpfad - Muss im nachhinein entfernt ODER "" gesetzt werden.
        protected const string IMAGEPATH = "D:\\Bilder\\Projekte\\MosaikGenerator\\";

        // Mögliche Typen von Konsolenausgaben
        private enum ConsolePrintTypes
        {
            WARNING,
            ERROR,
            INFO
        }

        /// <summary>
        /// Methode schneidet den mittleren Teil aus einem Basismotiv aus
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <returns>Void</returns>
        public void cropRect(int imageId)
        {
            printToConsole("Start cropping rect ...", ConsolePrintTypes.INFO);
            Images image = db.ImagesSet.Where(p => p.Id == imageId).First();
            int lowerSide = Math.Min(image.heigth, image.width);
            crop(imageId, lowerSide, lowerSide, CropModiTypes.MIDDLE);
        }

        /// <summary>
        /// Methode schneidet einen Teil aus einem Basismotiv aus
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <param name="width">Breite des Ausschnittes</param>
        /// <param name="height">Höhe des Ausschnittes</param>
        /// <param name="mode">Gibt an wo der Bildausschnitt starten soll</param>
        /// <returns>Erfolg oder misserfolg</returns>
        public bool crop(int imageId, int width, int height, CropModiTypes mode)
        {
            printToConsole("Start cropping ...", ConsolePrintTypes.INFO);

            printToConsole("Cropping to " + width + "x" + height, ConsolePrintTypes.INFO);

            try
            {
                String imagePath = db.ImagesSet.Where(p => p.Id == imageId).First().path;
                String imageFileName = db.ImagesSet.Where(p => p.Id == imageId).First().filename;

                // Crop image on filesystem
                Bitmap originalBitmap = openImage(imageId);
                if (originalBitmap == null)
                {
                    printToConsole("Cropping faild!", ConsolePrintTypes.WARNING);
                    return false;
                }

                Bitmap newBitmap = new Bitmap(originalBitmap.Width, originalBitmap.Height);

                int deltaWidth = originalBitmap.Width - width;
                int deltaHeight = originalBitmap.Height - height;
                int cropLeft = deltaWidth / 2;
                int cropTop = deltaHeight / 2;

                printToConsole("Cropmode: " + mode, ConsolePrintTypes.INFO);

                switch (mode)
                {
                    case CropModiTypes.MIDDLE:
                        newBitmap = originalBitmap.Clone(new Rectangle(cropLeft, cropTop, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.CENTERLEFT:
                        newBitmap = originalBitmap.Clone(new Rectangle(0, cropTop, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.CENTERRIGHT:
                        newBitmap = originalBitmap.Clone(new Rectangle(deltaWidth, cropTop, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.TOPLEFT:
                        newBitmap = originalBitmap.Clone(new Rectangle(0, 0, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.TOPRIGHT:
                        newBitmap = originalBitmap.Clone(new Rectangle(width, 0, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.BOTTOMLEFT:
                        newBitmap = originalBitmap.Clone(new Rectangle(0, height, width, height), originalBitmap.PixelFormat);
                        break;
                    case CropModiTypes.BOTTOMRIGHT:
                        newBitmap = originalBitmap.Clone(new Rectangle(width, height, width, height), originalBitmap.PixelFormat);
                        break;
                }

                originalBitmap.Dispose();
                printToConsole("Save file to: " + IMAGEPATH + imagePath + imageFileName, ConsolePrintTypes.INFO);
                newBitmap.Save(IMAGEPATH + imagePath + imageFileName);

                // Set new values in database    
                Images img = db.ImagesSet.Where(p => p.Id == imageId).First();
                printToConsole("Set image width to: " + newBitmap.Width, ConsolePrintTypes.INFO);
                img.width = newBitmap.Width;
                printToConsole("Set image heigth to: " + newBitmap.Height, ConsolePrintTypes.INFO);
                img.heigth = newBitmap.Height;
                db.SaveChanges();

                newBitmap.Dispose();

                printToConsole("Image: " + img.path + "/" + img.filename + " croped successfully.", ConsolePrintTypes.INFO);
                return true;
            }
            catch(Exception e)
            {
                printToConsole(e.ToString(), ConsolePrintTypes.WARNING);
                printToConsole("Cropping faild!", ConsolePrintTypes.WARNING);
                return false;
            }       
        }

        /// <summary>
        /// Skalliert ein Basismotiv auf eine gewünschte Größe 
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <param name="width">Breite des Bildes</param>
        /// <param name="height">Höhe des Bildes (opt.)</param>
        /// <returns>Erfolg oder misserfolg</returns>
        public bool scale(int imageId, int width, int height = 0)
        {
            if (height == 0)
                height = width;

            printToConsole("Start scaling ...", ConsolePrintTypes.INFO);

            printToConsole("Scaling to " + width + "x" + height, ConsolePrintTypes.INFO);

            try
            {
                String imagePath = db.ImagesSet.Where(p => p.Id == imageId).First().path;
                String imageFileName = db.ImagesSet.Where(p => p.Id == imageId).First().filename;

                // Scale image on filesystem 
                Bitmap originalBitmap = openImage(imageId);
                if (originalBitmap == null)
                {
                    printToConsole("Scaling faild!", ConsolePrintTypes.WARNING);
                    return false;
                }

                Bitmap newBitmap = new Bitmap(width, height);

                Graphics grafic = Graphics.FromImage(newBitmap);
                grafic.DrawImage(originalBitmap, new Rectangle(0, 0, width, height));

                originalBitmap.Dispose();
                grafic.Dispose();
                printToConsole("Save image to:" + IMAGEPATH + imagePath + imageFileName, ConsolePrintTypes.INFO);
                newBitmap.Save(IMAGEPATH + imagePath + imageFileName);

                // Set new values in database
                Images img = db.ImagesSet.Where(p => p.Id == imageId).First();
                printToConsole("Set image width to: " + newBitmap.Width, ConsolePrintTypes.INFO);
                img.width = newBitmap.Width;
                printToConsole("Set image heigth to: " + newBitmap.Height, ConsolePrintTypes.INFO);
                img.heigth = newBitmap.Height;
                db.SaveChanges();

                newBitmap.Dispose();

                unlogImage(imageId);

                return true;
            }
            catch(Exception e)
            {
                printToConsole(e.ToString(), ConsolePrintTypes.WARNING);
                printToConsole("Scaling faild!", ConsolePrintTypes.WARNING);
                return false;
            }
        }

        /// <summary>
        /// Sperrt ein Bild fürs lesen und schreiben
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <returns>Erfolg oder misserfolg</returns>
        private static bool lockImage(int imageId)
        {
            Motive bm = db.ImagesSet.OfType<Motive>().Where(p => p.Id == imageId).First();

            if (bm.readlock || bm.writelock)
            {
                printToConsole("Image: " + bm.path + "/" + bm.filename + " NOT locked", ConsolePrintTypes.INFO);
                return false;
            }

            bm.readlock = true;
            bm.writelock = true;

            printToConsole("Image: " + bm.path + "/" + bm.filename + " locked", ConsolePrintTypes.INFO);

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Gibt ein Bild wieder frei
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <returns>Void</returns>
        private void unlogImage(int imageId)
        {
            Motive bm = db.ImagesSet.OfType<Motive>().Where(p => p.Id == imageId).First();
            bm.readlock = false;
            bm.writelock = false;

            db.SaveChanges();
        }

        /// <summary>
        /// Methode löscht Bilder vom Filesystem und aus der DB
        /// </summary>
        /// <param name="imageId">ID des Bildes in der Datenbank</param>
        /// <returns>Void</returns>
        public void deleteFile(int imageId)
        {
            // Delete from database
            db.ImagesSet.Remove(db.ImagesSet.Where(p => p.Id == imageId).First());
            db.SaveChanges();

            // Delete from filesystem
            File.Delete(db.ImagesSet.Where(p => p.Id == imageId).First().path);
        }

        /// <summary>
        /// Methode öffnet Bilder und gibt diese als Bitmap zurück
        ///  Error-Handling mit Try-Catch
        /// </summary>
        /// <param name="fileID">ID des Bildes in der Datenbank</param>
        /// <returns>Bitmap des Bildes</returns>
        private static Bitmap openImage(int fileID)
        {
            // Lade das eine Bild aus der Datenbank
            Images bild = db.ImagesSet.Find(fileID);

            // Wenn kein Bild gefunden wurde soll 'null' returned werden
            if (bild == null)
            {
                printToConsole("Kein Bild in der Datenbank!", ConsolePrintTypes.ERROR);
                return null;
            }

            // Initialisiere die Bitmap
            Bitmap bmp = null;

            try
            {
                bmp = new Bitmap(IMAGEPATH + bild.path + bild.filename);
            }
            catch (System.ArgumentException)
            {
                printToConsole("Kein Bild in der Filesystem!", ConsolePrintTypes.ERROR);
                return null;
            }
            catch (Exception)
            {
                printToConsole("Unbekannter Fehler!", ConsolePrintTypes.ERROR);
                return null;
            }

            return bmp;
        }

        /// <summary>
        /// Gibt einen Text formatiert in der Konsole aus
        /// </summary>
        /// <param name="printText">Der auszugebende Text</param>
        /// <param name="errorType">Die Art der Konsolenausgabe (Error, Info, Warning...)</param>
        private static void printToConsole(string printText, ConsolePrintTypes messageType)
        {
            if (1 == 1)
            {
                switch (messageType)
                {
                    case ConsolePrintTypes.INFO:
                        Console.WriteLine("[INF] " + printText);
                        break;

                    case ConsolePrintTypes.ERROR:
                        Console.WriteLine("[ERR] " + printText);
                        break;

                    case ConsolePrintTypes.WARNING:
                        Console.WriteLine("[-!-] " + printText);
                        break;
                }
            }
        }
    }
}
