using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Datenbank.DAL;

namespace Mosaikgenerator
{
    class Program
    {
        /*=====Konstanten=====*/
        // "Datenbankverbindung"
        private static DBModelContainer db = new DBModelContainer();

        // DEBUGGING
        // Statischer Bilderpfad - Muss im nachhinein entfernt ODER "" gesetzt werden.
        private const string IMAGEPATH = "D:\\Bilder\\Projekte\\MosaikGenerator\\";

        // DEBUGGING
        // Konsolenausgaben (de-)aktvieren wenns gewünscht ist
        private const Boolean consoleOutput = true;

        // Mögliche Typen von Konsolenausgaben
        private enum ConsolePrintTypes
        {
            WARNING,
            ERROR,
            INFO
        }

        /// <summary>
        /// Main - Hier werden Testroutinen genutzt
        /// </summary>
        /// <param name="args">-/-</param>
        static void Main(string[] args)
        {
            // DEBUGGING
            // Pre Initialisierung der Datenbank (Nur nach neuaufsetzen nutzbar)
            preStorage();

            // DEBUGGING
            // Testfunktionen fuer die Datenbank (nur zum lernen :) )
            //testDatabase();

            // DEBUGGING
            // Gibt die Durchschnittsfarben der einzelnen Kacheln an
            AVGKachel();

            // DEBUGGING
            // Testweiser Mosaikgenerator der das "Mario" Basismotiv mit dem Pool in dem sich auch "KachelA" befindet in Pool 3 speichert
            //mosaikGenerator(db.ImagesSet.Where(p => p.displayname == "Mario").First().Id, db.ImagesSet.Where(p => p.displayname == "kachelA").First().PoolsId, 3, true, 1);

            // Beende das Programm erst auf Enter
            Console.WriteLine();
            Console.WriteLine("Enter to Exit");
            Console.ReadLine();
        }

        /*=====Weitere Funktionen=====*/
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
            if (consoleOutput)
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

        /// <summary>
        /// Der eigentliche Mosaikgenerator. Liest Kacheln aus und fuegt diese an die passenden Stellen fuer das Originalbild ein
        /// </summary>
        /// <param name="basisMotivID">Die ID des Basismotivs</param>
        /// <param name="kachelPoolID">Die ID des Kachelpools</param>
        /// <param name="mosaikPoolID">Die ID des Mosaikpools (in dem das Bild gespeichert wird)</param>
        /// <param name="kachelnMultiUseEnabled">Sollen Kacheln mehrfach genutzt werden duerfen?</param>
        /// <param name="auswahlNBesteKacheln">Aus wievielen der besten Bilder soll ein zufaelliges ausgewaehlt werden?</param>
        public static void mosaikGenerator(int basisMotivID, int kachelPoolID, int mosaikPoolID, Boolean kachelnMultiUseEnabled = true, int auswahlNBesteKacheln = 1)
        {
            // Pruefe ob alle wichtigen Parameter richtig gesetzt sind
            if (basisMotivID < 1 && kachelPoolID < 1 && mosaikPoolID < 1 && auswahlNBesteKacheln < 1)
            {
                printToConsole("Falscher Parameter - Abbruch! (" + basisMotivID + "--" + kachelPoolID + "--" + mosaikPoolID + "--" + auswahlNBesteKacheln + ")", ConsolePrintTypes.ERROR);
                return;
            }

            // Lade das Basismotiv in eine Bitmap
            Bitmap basisMotiv = openImage(basisMotivID);

            // Wenn basisMotiv null ist dann gab es einen Fehler beim laden des Bildes
            if (basisMotiv == null)
            {
                printToConsole("Basismotiv nicht gefunden - Abbruch!", ConsolePrintTypes.ERROR);
                return;
            }

            // Lade die Kachel-Poolinformationen aus der Datenbank
            Pools kachelPool = db.PoolsSet.Find(kachelPoolID);

            // Berechne die Anzahl aller Pixel
            int pixel = basisMotiv.Height * basisMotiv.Width;

            // Prüfe ob genug Kacheln im Pool sind
            // Wenn Kacheln nur einmalig erlaubt sind, muss geprueft werden ob genug vorhanden sind
            if (!kachelnMultiUseEnabled)
            {
                // Wenn weniger Kacheln als Pixel vorhanden sind...
                if (db.ImagesSet.Count<Images>() < pixel)
                {
                    basisMotiv.Dispose();
                    printToConsole("Zu wenig Kacheln im Pool!", ConsolePrintTypes.ERROR);
                    return;
                }
            }

            // lege ein neues Bild mit der neuen Groesse an
            // Berechnung in der Dokumentation
            Bitmap mosaik = new Bitmap((basisMotiv.Size.Width * kachelPool.size), (basisMotiv.Size.Height * kachelPool.size));

            // Falls Kacheln nur einmalig genutzt werden duerfen, muessen diese vermerkt werden
            List<int> usedKacheln = new List<int>();

            // Es soll eine Liste vorhanden sein die aus den N besten Bildern ein zufaelliges auswaehlt
            Dictionary<int, double> nKacheln = new Dictionary<int, double>();

            // Hole alle Bilder aus dem Pool
            Kacheln[] poolBilder = db.ImagesSet.OfType<Kacheln>().Where(p => p.PoolsId == kachelPoolID).ToArray();

            // Lege die Variablen zur Berechnung an
            Color pixelFarbe;
            Random rnd = new Random();
            Bitmap thisKachel;
            int bestfit;
            int differenzRot = 0;
            int differenzGruen = 0;
            int differenzBlau = 0;

            // gehe jeden einzelnen Pixel des Originalbildes durch
            for (int i = 1; i < basisMotiv.Size.Width + 1; i++)
            {
                for (int j = 1; j < basisMotiv.Size.Height + 1; j++)
                {
                    // Lade die Farbwerte des aktuellen Pixels
                    pixelFarbe = basisMotiv.GetPixel(i - 1, j - 1);

                    // Gehe jedes Bild im Pool durch und pruefe ob es gut dorthin passt
                    for (int k = 0; k < poolBilder.Length; k++)
                    {
                        // Wenn Kacheln Multi disabled ist & die Kachel schonmal 
                        // genutzt wurde soll der folgende Teil ignoriert werden
                        if (kachelnMultiUseEnabled || (!kachelnMultiUseEnabled && !usedKacheln.Contains(poolBilder[k].Id)))
                        {
                            // Berechne die drei jeweiligen (positiven) Differenzen
                            if (poolBilder[k].avgR > pixelFarbe.R)
                            {
                                differenzRot = poolBilder[k].avgR - pixelFarbe.R;
                            }
                            else {
                                differenzRot = pixelFarbe.R - poolBilder[k].avgR;
                            }

                            if (poolBilder[k].avgG > pixelFarbe.G)
                            {
                                differenzGruen = poolBilder[k].avgG - pixelFarbe.G;
                            }
                            else {
                                differenzGruen = pixelFarbe.G - poolBilder[k].avgG;
                            }

                            if (poolBilder[k].avgB > pixelFarbe.B)
                            {
                                differenzBlau = poolBilder[k].avgB - pixelFarbe.B;
                            }
                            else {
                                differenzBlau = pixelFarbe.B - poolBilder[k].avgB;
                            }

                            // Rechne den Farbabstand aus (Formel aus den Hinweisen zur Hausarbeit)
                            double farbAbstand = Math.Sqrt((double)(differenzRot * differenzRot) + (differenzGruen * differenzGruen) + (differenzBlau * differenzBlau));

                            // Wenn noch Platz in dem N-Kachel-Array ist, lege den Wert ab
                            if (auswahlNBesteKacheln > nKacheln.Count)
                            {
                                nKacheln.Add(poolBilder[k].Id, farbAbstand);
                            }
                            else
                            {
                                // Ermittle den schlechtesten Wert in den N-Kacheln
                                double schlechtesterWert = 0;
                                int index = -1;

                                // Gehe alle Elemente durch und finde den schlechtesten Wert
                                // Dieser muss GRÖSSER als die anderen sein, da der ABSTAND möglichst niedrig sein soll
                                foreach (var nKachel in nKacheln)
                                {
                                    if (nKachel.Value > schlechtesterWert)
                                    {
                                        index = nKachel.Key;
                                        schlechtesterWert = nKachel.Value;
                                    }
                                }

                                // Wenn das ergebnis besser ist als der schlechteste Wert
                                if (farbAbstand < schlechtesterWert)
                                {
                                    // Entferne das schlechteste Element
                                    nKacheln.Remove(index);

                                    // Fuege die neue nKachel hinzu
                                    nKacheln.Add(poolBilder[k].Id, farbAbstand);
                                }
                            }
                        }
                    }

                    // Hier wurden alle Bilder im Pool für einen Pixel durchsucht
                    // Hole nur die Keys aus der Liste
                    List<int> keys = Enumerable.ToList(nKacheln.Keys);

                    // Waehle ein zufaelligen Key - und damit auch das einzusetzende Bild
                    bestfit = keys[rnd.Next(nKacheln.Count)];

                    // Bereinige die NKacheln damit sie beim nächsten durchlauf nicht nochmal vorkommen :D
                    nKacheln.Clear();

                    // Wenn Kacheln nur einmal genutzt werden sollen, muss diese Kachel gespeichert werden
                    if (!kachelnMultiUseEnabled)
                    {
                        usedKacheln.Add(bestfit);
                    }

                    // Lade die Kachel
                    thisKachel = openImage(bestfit);

                    // Wenn basisMotiv null ist dann gab es einen Fehler beim laden des Bildes
                    if (thisKachel == null)
                    {
                        printToConsole("Kachel (" + bestfit + ") nicht gefunden - Abbruch!", ConsolePrintTypes.ERROR);
                        return;
                    }

                    // Füge nun jeden einzelnen Pixel an seiner dafür vorgesehnen Position ein
                    for (int x = 0; x < kachelPool.size; x++)
                    {
                        for (int y = 0; y < kachelPool.size; y++)
                        {
                            // Lade die Farbwerte des aktuellen Pixels
                            pixelFarbe = thisKachel.GetPixel(x, y);

                            // Und Fuege in an die richtige Position im neuen Mosaikbild ein
                            mosaik.SetPixel((((i - 1) * kachelPool.size) + x), (((j - 1) * kachelPool.size) + y), pixelFarbe);

                        }
                    }
                }
            }


            // Generiere eine Einzigartige Bezeichnung
            String UUID = Guid.NewGuid().ToString();

            // Speichere das Bild ab
            mosaik.Save(IMAGEPATH + "Mosaike\\" + UUID + ".png");

            var image = db.Set<Images>();
            image.Add(new Images { displayname = "Mosaik", filename = UUID + ".png", path = "Mosaike\\", heigth = (basisMotiv.Size.Height * kachelPool.size), width = (basisMotiv.Size.Width * kachelPool.size), hsv = "000", PoolsId = mosaikPoolID });

            // Mosaik fertig :)

            // Speichere die DB
            db.SaveChanges();
            //db.ImagesSet.Add(new Images())
        }

        // DEBUGGING

        /// <summary>
        /// Speichert vordefinierte Informationen in der Datenbank
        /// Nach JEDEM Datenbank-Update muss das hier ueberprueft werden!
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="poolMotive"></param>
        /// <param name="poolKacheln"></param>
        /// <param name="poolMosaike"></param>
        private static void preStorage(String username = "Demo", String poolMotive = "Basismotive", String poolKacheln = "Kacheln", String poolMosaike = "Mosaike")
        {
            // Speichert drei Pools fuer den Nutzer
            db.PoolsSet.Add(new Pools { name = "Basismotive", owner = username, size = 0, writelock = false });
            db.PoolsSet.Add(new Pools { name = "Kacheln", owner = username, size = 10, writelock = false });
            db.PoolsSet.Add(new Pools { name = "Mosaike", owner = username, size = 0, writelock = false });
            db.SaveChanges();

            db.Set<Motive>().Add(new Motive { path = "Basismotive\\", filename = "apple.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolMotive).First().Id, displayname = "Apfel", heigth = 64, width = 64, hsv = "0", readlock = false, writelock = false });
            db.Set<Motive>().Add(new Motive { path = "Basismotive\\", filename = "mario.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolMotive).First().Id, displayname = "Mario", heigth = 64, width = 64, hsv = "0", readlock = false, writelock = false });

            db.Set<Kacheln>().Add(new Kacheln { path = "Kacheln\\", filename = "kachelA.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolKacheln).First().Id, displayname = "KachelA", heigth = 10, width = 10, hsv = "0", avgR = 237, avgG = 28, avgB = 36 });
            db.Set<Kacheln>().Add(new Kacheln { path = "Kacheln\\", filename = "kachelB.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolKacheln).First().Id, displayname = "KachelB", heigth = 10, width = 10, hsv = "0", avgR = 0, avgG = 162, avgB = 231 });
            db.Set<Kacheln>().Add(new Kacheln { path = "Kacheln\\", filename = "kachelC.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolKacheln).First().Id, displayname = "KachelC", heigth = 10, width = 10, hsv = "0", avgR = 255, avgG = 242, avgB = 0 });
            db.Set<Kacheln>().Add(new Kacheln { path = "Kacheln\\", filename = "kachelD.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolKacheln).First().Id, displayname = "KachelD", heigth = 10, width = 10, hsv = "0", avgR = 181, avgG = 230, avgB = 29 });
            db.Set<Kacheln>().Add(new Kacheln { path = "Kacheln\\", filename = "kachelE.png", PoolsId = db.PoolsSet.Where(p => p.owner == username && p.name == poolKacheln).First().Id, displayname = "KachelE", heigth = 10, width = 10, hsv = "0", avgR = 163, avgG = 73, avgB = 164 });

            // Speichere die DB
            db.SaveChanges();
        }

        // DEBUGGING
        /// <summary>
        /// Spielwiese fuer einige Tests mit der Datenbank (Joins, Counts etc.)
        /// </summary>
        private static void testDatabase()
        {
            // Gibt die Anzahl aller Eintraege in Images aus
            Console.WriteLine("x" + db.ImagesSet.Count<Images>() + "y");

            // Gibt die Anzahl aller Eintraege in Images aus
            Console.WriteLine("x" + db.ImagesSet.OfType<Kacheln>().Count<Images>() + "y");

            // Holt die Kachel mit der ID 5
            var k = db.ImagesSet.OfType<Kacheln>().Where(p => p.Id == 5);

            // Gibt das Element aus 
            Console.WriteLine("x" + k.First() + "y");

            // Gibt Informationen ueber alle Kacheln aus
            Console.WriteLine("Kachels only: ");
            foreach (var kachel in db.ImagesSet.OfType<Kacheln>())
            {
                Console.WriteLine("    {0} {1} {2}", kachel.avgR, kachel.avgG, kachel.avgB);
            }

            // Gibt die AVGR der Kachel mit der ID 4 aus
            Console.WriteLine("x" + db.ImagesSet.OfType<Kacheln>().Where(p => p.Id == 4).First().avgR + "y");
        }

        // DEBUGGING
        private static void AVGKachel()
        {
            Kacheln[] poolBilder = db.ImagesSet.OfType<Kacheln>().Where(p => p.PoolsId == 2).ToArray();
            foreach (Kacheln img in poolBilder)
            {
                Bitmap datei = openImage(img.Id);
                double red = 0;
                double green = 0;
                double blue = 0;
                var ges = datei.Width * datei.Height;
                for (int i = 0; i < datei.Width; i++)
                {
                    for (int j = 0; j < datei.Height; j++)
                    {
                        Color rgb = datei.GetPixel(i, j);
                        red += rgb.R;
                        green += rgb.G;
                        blue += rgb.B;
                    }
                }
                red = red / ges;
                green = green / ges;
                blue = blue / ges;

                // Speichere diese AVG-Werte in der Datenbank
                db.Set<Kacheln>().Where(p => p.Id == img.Id).Single().avgR = (int)red;
                db.Set<Kacheln>().Where(p => p.Id == img.Id).Single().avgG = (int)green;
                db.Set<Kacheln>().Where(p => p.Id == img.Id).Single().avgB = (int)blue;
                db.SaveChanges();

                Console.WriteLine(img.displayname + ": (" + red + " -- " + green + " -- " + blue + ")");
            }
        }
    }
}
