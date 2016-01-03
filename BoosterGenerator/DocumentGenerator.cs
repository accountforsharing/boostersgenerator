using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using iTextSharp.text;
using iTextSharp.text.pdf;

using Font = iTextSharp.text.Font;
using Image = iTextSharp.text.Image;
using Rectangle = iTextSharp.text.Rectangle;

namespace BoosterGenerator
{
    public class DocumentGenerator
    {
        private Random _rnd = new Random();

        public void Generate(string setCode = null, Settings settings = null)
        {
            if (!Directory.Exists("docs"))
            {
                Directory.CreateDirectory("docs");
            }

            var targetWidth = Utilities.InchesToPoints(2.5f);
            var targetHeight = Utilities.InchesToPoints(3.5f);

            var pageW = PageSize.A4.Width;
            var pageH = PageSize.A4.Height;

            var marginW = (pageW - 3 * targetWidth) / 2;
            var marginH = (pageH - 3 * targetHeight) / 2;

            int[] grays = { /*16, 32,*/ 48 };
            float[] borders = { 3, 4, 5/*, 6*/ };
            int[] landsCounts = { 30, 41, 50 };
            bool[] fulls = { false, true };
            float[] sxs = { 1, 0, -1 };
            float[] sys = { 4.5f, 3, 1.5f, 0, -2, -4.5f };

            var setName = setCode ?? "RAV";

            settings = settings ?? Settings.Standart();
            settings.Set = setName;
            //settings.MergePages = false;
            //settings.BoosterDir = "specialbooster";
            //settings.BoosterDir = "empty";
            BuildDoc(settings, targetWidth, targetHeight, marginW, marginH, 19, keepBoosters: false, shiftX: 0, shiftY: 0);

            settings = Settings.Standart();
            settings.Set = setName;
            settings.MergePages = false;
            //settings.BoosterDir = "specialbooster";
            //settings.BoosterDir = "empty";
            BuildDoc(settings, targetWidth, targetHeight, marginW, marginH, 19, keepBoosters: false, shiftX: 0, shiftY: 0);

            settings = Settings.StandartHand();
            settings.Set = setName;
            //settings.MergePages = false;
            //settings.BoosterDir = "specialbooster";
            //settings.BoosterDir = "empty";
            BuildDoc(settings, targetWidth, targetHeight, marginW, marginH, 19, keepBoosters: false, shiftX: 0, shiftY: 0);

            /*foreach (var gray in grays)
            {
                foreach (var border in borders)
                {
                    foreach (var sx in sxs)
                    {
                        foreach (var sy in sys)
                        {
                            var settings = Settings.Standart();
                            BuildDoc(settings, targetWidth, targetHeight, marginW, marginH, boostersCount: 19, gray: gray, border: border,
                                keepBoosters: true, shiftX: sx, shiftY: sy);
                        }
                    }

                    /*foreach (var landsCount in landsCounts)
                    {
                        foreach (var full in fulls)
                        {#1#
                    //BuildDoc(targetWidth, targetHeight, marginW, marginH, 19, gray, border);
/*                        }
                    }#1#
                }
            }*/
        }

        private void BuildDoc(
            Settings settings,
            float targetWidth,
            float targetHeight,
            float marginW,
            float marginH,
            int? boostersCount = null,
            int? gray = null,
            float? border = null,
            bool? keepBoosters = null,
            int? landsCount = null,
            bool? landsFull = null,
            float? shiftX = null,
            float? shiftY = null)
        {
            if (boostersCount != null)
            {
                settings.BoostersCount = boostersCount.Value;
            }
            if (gray != null)
            {
                settings.BorderGray = gray.Value;
            }
            if (border != null)
            {
                settings.Border = border.Value;
            }
            if (landsCount != null)
            {
                settings.LandsPerTypeCount = landsCount.Value;
            }
            if (landsFull != null)
            {
                settings.BoosterImageDir = landsFull.Value ? "lands/full" : "lands/part";
            }
            if (keepBoosters != null)
            {
                settings.KeepBoosters = keepBoosters.Value;
            }
            if (shiftX != null)
            {
                settings.ShiftX = shiftX.Value;
            }
            if (shiftY != null)
            {
                settings.ShiftY = shiftY.Value;
            }

            settings.CellWidth = targetWidth;
            settings.CellHeight = targetHeight;
            settings.MarginW = marginW;
            settings.MarginH = marginH;

            var docName = BuildDocName(settings);
            var fileStream = File.Create(string.Format("docs/{0}.pdf", docName));
            var pageSize = new Rectangle(PageSize.A4);
            Document doc = new Document(pageSize);
            PdfWriter writer = PdfWriter.GetInstance(doc, fileStream);

            var pages = CreatePages(settings);


            try
            {
                BuildDocument(doc, pages, settings);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                throw;
            }
        }

        private string BuildDocName(Settings settings)
        {
            var sb = new StringBuilder("doc");
            if (settings.Set != null)
            {
                sb.Append(string.Format("_{0}", settings.Set));
            }
            if (!settings.Lands)
            {
                sb.Append(string.Format("_n{0}", settings.BoostersCount));
            }
            else
            {
                sb.Append(string.Format("_lands{0}", settings.LandsPerTypeCount));
            }
            sb.Append(string.Format("_b{0}", settings.Border));
            sb.Append(string.Format("_g{0}", settings.BorderGray));
            if (settings.Lands)
            {
                if (settings.BoosterImageDir.ToLower().Contains("full"))
                {
                    sb.Append("_full");
                }
                else
                {
                    sb.Append("_common");
                }
            }
            sb.Append(string.Format("_({0}, {1})", settings.ShiftX, settings.ShiftY));
            if (!settings.MergePages)
            {
                sb.Append("_nomerge");
            }
            if (settings.SkipGrid)
            {
                sb.Append("_nogrid");
            }
            return sb.ToString();
        }

        private List<Page> CreatePages(Settings settings)
        {
            List<List<string>> boosters;
            if (settings.Lands)
            {
                boosters = GetLandBoosters(settings);
            }
            else
            {
                boosters = GetBoosters(settings);
            }

            PagePointer p = new PagePointer();
            List<Page> imagePages = new List<Page>();
            List<Page> textPages = new List<Page>();
            Page imagePage = new Page();
            Page textPage = new Page();
            for (int i = 0; i < boosters.Count; i++)
            {
                var booster = boosters[i];
                foreach (var card in booster)
                {
                    if (p.Inc())
                    {
                        imagePage = new Page { IsGrid = false };
                        imagePages.Add(imagePage);
                        textPage = new Page { IsGrid = true };
                        textPages.Add(textPage);
                    }

                    imagePage.Images[p.I, p.J] = card;
                    textPage.Texts[p.I, 2 - p.J] = settings.Lands ? "L" : GetText(i + 1);
                }
                if (!settings.MergePages)
                {
                    p.SetEnd();
                }
            }

            List<Page> pages = new List<Page>();
            if (settings.NormalOrder)
            {
                for (int i = 0; i < imagePages.Count; i++)
                {
                    pages.Add(imagePages[i]);
                    pages.Add(textPages[i]);
                }
            }
            else
            {
                pages.AddRange(imagePages);
                pages.AddRange(textPages);
            }

            return pages;
        }

        private List<List<string>> GetLandBoosters(Settings settings)
        {
            var landTypes = new[] { "Plains", "Island", "Swamp", "Mountain", "Forest" };
            var lands = new List<string>();
            foreach (var landType in landTypes)
            {
                for (int i = 0; i < settings.LandsPerTypeCount; i++)
                {
                    lands.Add(landType);
                }
            }
            return new List<List<string>> { lands };
        }

        private static List<List<string>> GetBoosters(Settings settings)
        {
            int boostersCount = settings.BoostersCount;
            var boosterDir = settings.BoosterDir ?? string.Format("boosters\\{0}", settings.Set);
            var boostersFiles = Directory.GetFiles(boosterDir).Reverse().ToList();

            List<List<string>> boosters = new List<List<string>>();
            for (int i = 0; i < boostersCount; i++)
            {
                if (!boostersFiles.Any())
                {
                    break;
                }

                var file = boostersFiles.Last();
                boostersFiles.Remove(file);

                var names = File.ReadAllLines(file);
                boosters.Add(new List<string>(names.Where(n => !string.IsNullOrWhiteSpace(n))));

                if (!settings.KeepBoosters)
                {
                    File.Delete(file);
                }
            }
            return boosters;
        }

        private string GetText(int num)
        {
            var s = num.ToString();
            if (s.All(ch => ch == '6' || ch == '9'))
            {
                s += ".";
            }
            return s;
        }

        private List<Page> CreateSimplePages()
        {
            List<Page> pages = new List<Page>();

            Page page;

            //page = new Page { IsGrid = false, Images = RandomImages("BFZ") };
            page = new Page { IsGrid = false, Images = ImagesFromFile("booster.txt") };
            pages.Add(page);

            /*page = new Page { IsGrid = false, Images = SameImage("White.Full.jpg") };
            pages.Add(page);*/

            page = new Page { IsGrid = true, Texts = CreateTexts("3") };
            pages.Add(page);
            return pages;
        }

        private Image[,] SameImage(string path)
        {
            var images = new Image[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var imageStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    images[i,j] = Image.GetInstance(imageStream);
                }
            }
            return images;
        }

        private Image[,] RandomImages(string dir)
        {
            var rnd = _rnd;
            var files = Directory.GetFiles(dir, "*.jpg");
            var images = new Image[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var path = files[rnd.Next(files.Length)];
                    var imageStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    images[i,j] = Image.GetInstance(imageStream);
                }
            }
            return images;
        }

        private string[,] ImagesFromFile(string file)
        {
            var names = File.ReadAllLines(file);
            var images = new string[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    images[i,j] = string.Format("BFZ/{0}.Full.jpg", names[3 * i + j]);
                }
            }
            return images;
        }

        private string[,] CreateTexts(string s)
        {
            var texts = new string[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    texts[i, j] = s;
                }
            }
            return texts;
        }

        private int _counter = 0;
        private float[] sx = { 0, 1f, -1f };
        private float[] sy = { 0, 4.5f, -4.5f };

        private void AddPage(Document doc, bool isGrid, string[,] images, string[,] texts, Settings settings)
        {
            if (isGrid)
            {
                if (settings.SkipGrid)
                {
                    return;
                }

                //var shiftX = Utilities.MillimetersToPoints(settings.ShiftX);
                //var shiftY = Utilities.MillimetersToPoints(settings.ShiftY);

                /*var shiftX = Utilities.MillimetersToPoints(sx[_counter]);
                var shiftY = Utilities.MillimetersToPoints(sy[_counter]);
                _counter++;*/

                //doc.SetMargins(settings.MarginW + shiftX, settings.MarginW - shiftX, settings.MarginH + shiftY, settings.MarginH - shiftY);
                doc.SetMargins(settings.MarginW, settings.MarginW, settings.MarginH, settings.MarginH);
            }
            else
            {
                doc.SetMargins(settings.MarginW, settings.MarginW, settings.MarginH, settings.MarginH);
            }
            doc.NewPage();

            PdfPTable table = new PdfPTable(3);
            table.SpacingBefore = 0;
            table.SpacingAfter = 0;
            table.SetWidths(new[] { settings.CellWidth, settings.CellWidth, settings.CellWidth });
            table.WidthPercentage = 100;
            //table.TotalWidth = 3 * image.ScaledWidth;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    PdfPCell cell;
                    if (isGrid)
                    {
                        var text = texts[i, j];
                        if (!string.IsNullOrWhiteSpace(text) && settings.BackText)
                        {
                            cell = new PdfPCell(new Phrase(text, FontFactory.GetFont("Courier", 24, Font.NORMAL)));
                            cell.HorizontalAlignment = Element.ALIGN_CENTER;
                            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
                        }
                        else
                        {
                            cell = new PdfPCell();
                        }
                        cell.FixedHeight = settings.CellHeight;
                    }
                    else
                    {
                        var cardName = images[i, j];
                        if (string.IsNullOrWhiteSpace(cardName))
                        {
                            cell = new PdfPCell();
                            cell.BackgroundColor = BaseColor.WHITE;
                            cell.FixedHeight = settings.CellHeight;
                            cell.BorderColor = settings.Border > 0 ? BaseColor.BLACK : BaseColor.GRAY;
                        }
                        else
                        {
                            var image = GetImage(cardName, settings);

                            if (image == null)
                            {
                                string message = string.Format("Could not find image for '{0}'.", cardName);
                                throw new ApplicationException(message);
                            }

                            /*var borderWidth = settings.Border;
                            var borderHeight = settings.Border * settings.CellHeight / settings.CellWidth;
                            image.ScaleAbsolute(settings.CellWidth - 2 * borderWidth, settings.CellHeight - 2 * borderHeight);*/

                            image.ScaleToFit(settings.CellWidth - 2 * settings.Border, settings.CellHeight - 2 * settings.Border - (settings.Border > 0 ? 0 : 1));
                            var borderWidth = (settings.CellWidth - image.ScaledWidth) / 2;
                            var borderHeight = (settings.CellHeight - image.ScaledHeight) / 2;
                            /*if (settings.Border > 0)
                            {
                                image.Border = Rectangle.TOP_BORDER | Rectangle.RIGHT_BORDER | Rectangle.BOTTOM_BORDER | Rectangle.LEFT_BORDER;
                                image.BorderColor = BaseColor.GREEN;
                                image.BorderWidth = settings.Border;
                            }*/
                            cell = new PdfPCell(image);
                            //if (settings.Border > 0)
                            //{
                                cell.PaddingLeft = borderWidth;
                                cell.PaddingRight = borderWidth;
                                cell.PaddingTop = borderHeight;
                                cell.PaddingBottom = borderHeight;

                                //cell.BackgroundColor = BaseColor.DARK_GRAY;
                                cell.BorderColor = settings.Border > 0 ? BaseColor.BLACK : BaseColor.GRAY;

                                float k = settings.Border > 0 ? settings.OuterBorderMultiplier : 0;

                                float w = 3 * settings.CellWidth + 2 * k * borderWidth;
                                float h = 3 * settings.CellHeight + 2 * k * borderHeight;
                                var bitmap = new Bitmap(1, 1);
                                //var bitmap = new Bitmap((int) w, (int) h);
                                //bitmap.SetPixel(1, 1, Color.DarkGray);
                                //Image backgroundImage = Image.GetInstance(bitmap, BaseColor.LIGHT_GRAY);
                            //if (settings.Border > 0)
                            //{
                            var backgroundColor = settings.Border > 0 ? new GrayColor(settings.BorderGray) : BaseColor.BLACK;
                            Image backgroundImage = Image.GetInstance(bitmap, backgroundColor);
                                backgroundImage.Alignment = Image.UNDERLYING;
                                backgroundImage.ScaleAbsolute(w, h);
                                backgroundImage.SetAbsolutePosition(settings.MarginW - k * borderWidth, settings.MarginH - k * borderHeight);

                                /*backgroundImage.Border = Rectangle.TOP_BORDER | Rectangle.RIGHT_BORDER | Rectangle.BOTTOM_BORDER | Rectangle.LEFT_BORDER;
                                backgroundImage.BorderColor = BaseColor.BLACK;
                                backgroundImage.BorderWidth = 0.5f;*/

                                doc.Add(backgroundImage);
                            //}
                            //}
                            /*else
                            {
                                cell.BorderColor = BaseColor.WHITE;
                            }*/
                        }
                    }

                    table.AddCell(cell);
                }
            }
            //table.
            doc.Add(table);

            /*if (!isGrid)
            {
                settings.Border -= 1;
                settings.BorderGray += 16;
            }*/
        }

        private Image GetImage(string cardName, Settings settings)
        {
            var imagesDir = settings.BoosterImageDir ?? string.Format("images\\{0}", settings.Set);
            var files = Directory.GetFiles(imagesDir);
            var names = files.Select(f => new KeyValuePair<string, string>(ExtractName(f), f)).ToArray();
            var match = names.Where(n => n.Key.Equals(cardName, StringComparison.InvariantCultureIgnoreCase)).ToArray();
            if (match.Length == 0)
            {
                return null;
            }
            string imagePath;
            if (match.Length == 1)
            {
                imagePath = match[0].Value;
            }
            else
            {
                imagePath = match[_rnd.Next(match.Length)].Value;
            }
            //imagePath = "Empty\\Empty.jpg";
            if (!_imagesCache.ContainsKey(imagePath))
            {
                var imageStream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                _imagesCache[imagePath] = Image.GetInstance(imageStream);
            }
            return _imagesCache[imagePath];
        }

        private readonly Dictionary<string, Image> _imagesCache = new Dictionary<string, Image>();

        private static string ExtractName(string filePath)
        {
            var name = Path.GetFileName(filePath).Split('.').First();
            var match = Regex.Match(name, @"(\w+)\d+");
            if (match.Success)
            {
                name = match.Groups[1].Value;
            }
            return name;
        }

        public void BuildDocument(Document doc, IList<Page> pages, Settings settings)
        {
            doc.Open();

            for (int i = 0; i < pages.Count; i++)
            {
                AddPage(doc, pages[i].IsGrid, pages[i].Images, pages[i].Texts, settings);
            }

            doc.Close();
        }
    }


    public class PagePointer
    {
        public PagePointer()
        {
            SetEnd();
        }

        public int I { get; set; }
        public int J { get; set; }

        public bool Inc()
        {
            J++;
            if (J >= 3)
            {
                J = 0;
                I++;
                if (I >= 3)
                {
                    I = 0;
                    return true;
                }
            }
            return false;
        }

        public void SetEnd()
        {
            I = 2;
            J = 2;
        }
    }

    public class Page
    {
        public Page()
        {
            Images = new string[3,3];
            Texts = new string[3,3];
        }

        public bool IsGrid { get; set; }

        public string[,] Images { get; set; }

        public string[,] Texts { get; set; }
    }

    public class Settings
    {
        public float CellWidth { get; set; }

        public float CellHeight { get; set; }

        public float Border { get; set; }

        public float MarginW { get; set; }

        public float MarginH { get; set; }

        public float OuterBorderMultiplier { get; set; }

        public int BorderGray { get; set; }

        public string BoosterImageDir { get; set; }

        public bool NormalOrder { get; set; }

        public bool MergePages { get; set; }

        public int BoostersCount { get; set; }

        public bool BackText { get; set; }

        public bool Lands { get; set; }

        public int LandsPerTypeCount { get; set; }

        public bool KeepBoosters { get; set; }

        public string BoosterDir { get; set; }

        public float ShiftX { get; set; }

        public float ShiftY { get; set; }

        public string Set { get; set; }

        public bool SkipGrid { get; set; }

        public static Settings LandsCommon()
        {
            return new Settings
            {
                Border = BorderDefault,
                OuterBorderMultiplier = OuterBorderMultiplierDefault,
                BorderGray = BorderGrayDefault,
                //BoosterImageDir = "BFZ",
                NormalOrder = true,
                MergePages = true,
                BackText = true,
                Lands = true,
                LandsPerTypeCount = 40,
                KeepBoosters = true,
            };
        }

        public static Settings Standart()
        {
            return new Settings
            {
                Border = BorderDefault,
                OuterBorderMultiplier = OuterBorderMultiplierDefault,
                BorderGray = BorderGrayDefault,
                //BoosterDir = "boosters",
                //BoosterImageDir = "BFZ",
                NormalOrder = true,
                MergePages = true,
                BoostersCount = 19,
                BackText = true,
                KeepBoosters = false,
            };
        }

        public static Settings StandartHand()
        {
            return new Settings
            {
                Border = 0,
                OuterBorderMultiplier = OuterBorderMultiplierDefault,
                BorderGray = BorderGrayDefault,
                //BoosterDir = "boosters",
                //BoosterImageDir = "BFZ",
                NormalOrder = true,
                MergePages = false,
                BoostersCount = 19,
                BackText = true,
                KeepBoosters = false,
                SkipGrid = true,
            };
        }

        public static Settings Sample()
        {
            return new Settings
            {
                Border = BorderDefault,
                OuterBorderMultiplier = OuterBorderMultiplierDefault,
                BorderGray = BorderGrayDefault,
                BoosterDir = "specialbooster",
                //BoosterImageDir = "BFZ",
                NormalOrder = true,
                MergePages = true,
                BoostersCount = 1,
                BackText = true,
                KeepBoosters = true,
            };
        }

        public const float BorderDefault = 4;

        public const int BorderGrayDefault = 48;

        public const float OuterBorderMultiplierDefault = 1.5f;
    }
}
