﻿using Contracts;
using Datenbank.DAL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kachelgenerator
{
    class Generator : IKachelGenerator
    {
        // "Datenbankverbindung"
        private static DBModelContainer db = new DBModelContainer();

        // Statischer Bilderpfad
        private static string IMAGEPATH = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\VS16_MosaikGenerator\\";

        // Mögliche Typen von Konsolenausgaben
        private enum ConsolePrintTypes
        {
            WARNING,
            ERROR,
            INFO
        }

        /// <summary>
        /// Erstellt eine Kachel
        /// </summary>
        /// <param name="kachelPoolID">Der Pool in dem die Kachel gespeichert werden soll</param>
        /// <param name="r">Der Rot-Anteil der Kachelfarbe</param>
        /// <param name="g">Der Grün-Anteil der Kachelfarbe</param>
        /// <param name="b">Der Blau-Anteil der Kachelfarbe</param>
        /// <param name="nois">Es kann ein Rauschen der Farbwerte erzeugt werden</param>
        /// <returns>Void</returns>
        public void genKachel(int kachelPoolID, int r, int g, int b, bool nois=false)
        {
            int nr = r;
            int ng = g;
            int nb = b;
            int boul;
            int rand;

            printToConsole("Find pool by Id: " + kachelPoolID, ConsolePrintTypes.INFO);
            Pools kachelPool = db.PoolsSet.Where(p => p.Id == kachelPoolID).First();

            printToConsole("R: " + r + "G: " + g + "B: " + b + "Nois: " + nois, ConsolePrintTypes.INFO);

            printToConsole("Kachelsize " + kachelPool.size + "x" + kachelPool.size, ConsolePrintTypes.INFO);
            int width = kachelPool.size;
            int height = kachelPool.size;

            Random random = new Random();
            Bitmap bitmap = new Bitmap(width, height);

            try {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        if (nois)
                        {
                            boul = random.Next(0, 1);
                            rand = random.Next(50, 70);

                            if (boul == 1)
                            {
                                nr = r + rand;
                                ng = g + rand;
                                nb = b + rand;
                            }
                            else
                            {
                                nr = r - rand;
                                ng = g - rand;
                                nb = b - rand;
                            }

                            nr = minMax(nr);
                            ng = minMax(ng);
                            nb = minMax(nb);

                        }
                        bitmap.SetPixel(x, y, Color.FromArgb(nr, ng, nb));
                    }
                }
            }
            catch(Exception e)
            {
                printToConsole(e.ToString(), ConsolePrintTypes.INFO);
            }

            // Generiere eine Einzigartige Bezeichnung
            String UUID = Guid.NewGuid().ToString();
            printToConsole("gen UUID: " + UUID, ConsolePrintTypes.INFO);

            // Speichere das Bild ab
            bitmap.Save(IMAGEPATH + "Kacheln\\" + UUID + ".png");
            printToConsole("Save image to: " + IMAGEPATH + "Kacheln\\" + UUID + ".png", ConsolePrintTypes.INFO);

            var kachel = db.Set<Kacheln>();
            kachel.Add(new Kacheln
            {
                displayname = "Kachel",
                filename = UUID + ".png",
                path = "Kacheln\\",
                heigth = bitmap.Size.Height,
                width = bitmap.Size.Width,
                hsv = "000",
                PoolsId = kachelPoolID,
                avgR = (int)colorValue(bitmap, ColorType.RED),
                avgG = (int)colorValue(bitmap, ColorType.GREEN),
                avgB = (int)colorValue(bitmap, ColorType.BLUE)
            });

            // Speichere die DB
            db.SaveChanges();
            printToConsole("Generation done!", ConsolePrintTypes.INFO);

            bitmap.Dispose();
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

        /// <summary>
        /// Errechnet den durchschnittlichen Farbwert im RGB-Bereich
        /// </summary>
        /// <param name="bitmap">Die Bitmap die berechnet werden soll</param>
        /// <param name="type">Welcher der Farbwerte soll ausgegeben werden</param>
        /// <returns>Durchschnittlicher farbwert</returns>
        private static double colorValue(Bitmap bitmap, ColorType type)
        {
            printToConsole("Calculate RGB values", ConsolePrintTypes.INFO);

            double red = 0;
            double green = 0;
            double blue = 0;
            var ges = bitmap.Width * bitmap.Height;
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color rgb = bitmap.GetPixel(i, j);
                    red += rgb.R;
                    green += rgb.G;
                    blue += rgb.B;
                }
            }
            red = red / ges;
            green = green / ges;
            blue = blue / ges;

            switch (type)
            {
                case ColorType.RED:
                    return red;
                case ColorType.GREEN:
                    return green;
                case ColorType.BLUE:
                    return blue;
            }

            return 0;
        }

        /// <summary>
        /// Begrenzt die Eingabewerte auf min. 0 und max. 255
        /// </summary>
        /// <param name="value">Eingabewert</param>
        /// <returns>Den begrenzten Wert</returns>
        private static int minMax(int value)
        {
            if (value > 255)
            {
                printToConsole("Limiting " + value + " to 255" , ConsolePrintTypes.INFO);
                value = 255;
            }
            if (value < 0)
            {
                printToConsole("Limiting " + value + " to 0", ConsolePrintTypes.INFO);
                value = 0;
            }
            return value;
        }
    }
}
