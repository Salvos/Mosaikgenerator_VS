using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Datenbank.DAL;
using Contracts;

namespace Mosaikgenerator
{
    class Generator:IMosaikGenerator
    {
        /*=====Konstanten=====*/
        // "Datenbankverbindung"
        private static DBModelContainer db = new DBModelContainer();

        // Statischer Bilderpfad
        private static string IMAGEPATH = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\VS16_MosaikGenerator\\";

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
        public bool mosaikGenerator(int basisMotivID, int kachelPoolID, int mosaikPoolID, Boolean kachelnMultiUseEnabled = true, int auswahlNBesteKacheln = 1)
        {

            printToConsole("MosaikStart! (" + basisMotivID + "--" + kachelPoolID + "--" + mosaikPoolID + "--" + auswahlNBesteKacheln + ")", ConsolePrintTypes.INFO);

            // Pruefe ob alle wichtigen Parameter richtig gesetzt sind
            if (basisMotivID < 1 && kachelPoolID < 1 && mosaikPoolID < 1 && auswahlNBesteKacheln < 1)
            {
                printToConsole("Falscher Parameter - Abbruch! (" + basisMotivID + "--" + kachelPoolID + "--" + mosaikPoolID + "--" + auswahlNBesteKacheln + ")", ConsolePrintTypes.ERROR);
                return false;
            }

            // Lade das Basismotiv in eine Bitmap
            Bitmap basisMotiv = openImage(basisMotivID);

            // Wenn basisMotiv null ist dann gab es einen Fehler beim laden des Bildes
            if (basisMotiv == null)
            {
                printToConsole("Basismotiv nicht gefunden - Abbruch!", ConsolePrintTypes.ERROR);
                return false;
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
                    return false;
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
                        return false;
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
            mosaik.Save(IMAGEPATH + "Motive\\" + UUID + ".png");

            var image = db.Set<Motive>();
            image.Add(new Motive { displayname = "Mosaik", filename = UUID + ".png", path = "Motive\\", heigth = (basisMotiv.Size.Height * kachelPool.size), width = (basisMotiv.Size.Width * kachelPool.size), hsv = "000", PoolsId = mosaikPoolID, writelock=false });

            // Speichere die DB
            db.SaveChanges();

            return true;
        }
    }
}
