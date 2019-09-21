using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.IO;

namespace csharp
{
    public sealed class ChipImages
    {
        public const byte elementCount = 12;
        public const string aquaURL = "http://vignette.wikia.nocookie.net/megaman/images/f/fe/BC_Element_Aqua.png";

        public const string breakURL = "http://vignette.wikia.nocookie.net/megaman/images/0/0e/BC_Attribute_Break.png";

        public const string cursorURL = "http://vignette.wikia.nocookie.net/megaman/images/2/2b/TypeCursor.png";

        public const string elecURL = "http://vignette.wikia.nocookie.net/megaman/images/f/f6/BC_Element_Elec.png";

        public const string fireURL = "http://vignette.wikia.nocookie.net/megaman/images/3/38/BC_Element_Heat.png";

        public const string invisURL = "http://vignette.wikia.nocookie.net/megaman/images/e/e0/TypeInvis.png";

        public const string nullURL = "http://vignette.wikia.nocookie.net/megaman/images/4/47/BC_Element_Null.png";

        public const string objectURL = "http://vignette.wikia.nocookie.net/megaman/images/4/4c/TypeObstacle.png";

        public const string recoveryURL = "http://vignette.wikia.nocookie.net/megaman/images/8/81/TypeRecover.png";

        public const string swordURL = "http://vignette.wikia.nocookie.net/megaman/images/d/d5/BC_Attribute_Sword.png";

        public const string windURL = "http://vignette.wikia.nocookie.net/megaman/images/b/b1/BC_Attribute_Wind.png";

        public const string woodURL = "http://vignette.wikia.nocookie.net/megaman/images/8/83/BC_Element_Wood.png";

        private static readonly string[] URLs =
        {
            fireURL, aquaURL, elecURL, woodURL, windURL, swordURL, breakURL, cursorURL, recoveryURL, invisURL, objectURL, nullURL
        };

        private static readonly Lazy<ChipImages> lazy = new Lazy<ChipImages>(() => new ChipImages());

        private Bitmap[] images;

        private Dictionary<string[], Bitmap> combinedImages;

        public static ChipImages Instance
        {
            get
            {
                return lazy.Value;
            }
        }

        private ChipImages()
        {
            combinedImages = new Dictionary<string[], Bitmap>();
        }

        public async Task loadChipImages()
        {
            images = new Bitmap[elementCount];
            await Task.Run(() => Parallel.For(0, elementCount, async (index) =>
            {
                var imageStream = await Library.client.GetStreamAsync(URLs[index]);
                
                images[index] = new Bitmap(imageStream);
                //imageStream.Dispose();
            }));
        }

        public Bitmap getElement(string[] elements)
        {
            if (images == null) throw new NullReferenceException();
            if (elements.Length == 1)
            {
                return getElement(elements[0]);
            }
            else
            {
                if (combinedImages.TryGetValue(elements, out Bitmap toReturn))
                {
                    return toReturn;
                }
                return makeCombinedImage(elements);
            }
        }

        public Bitmap makeCombinedImage(string[] elem)
        {
            Bitmap[] imagesToCombine = new Bitmap[elem.Length];
            int width = 0;
            int height = 0;
            for (int i = 0; i < elem.Length; i++)
            {
                imagesToCombine[i] = getElement(elem[i]);
                width += imagesToCombine[i].Width;
                if (imagesToCombine[i].Height > height)
                {
                    height = imagesToCombine[i].Height;
                }
            }

            Bitmap img3 = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(img3);
            g.Clear(Color.Transparent);
            width = 0;
            for (int i = 0; i < elem.Length; i++)
            {
                g.DrawImage(imagesToCombine[i], new System.Drawing.Point(width, 0));
                width += imagesToCombine[i].Width;
            }
            g.Dispose();
            combinedImages.Add(elem, img3);
            return img3;
        }

        private Bitmap getElement(string elementToGet)
        {
            switch (elementToGet.ToLower())
            {
                case "fire":
                    return images[0];
                case "aqua":
                    return images[1];
                case "elec":
                    return images[2];
                case "wood":
                    return images[3];
                case "wind":
                    return images[4];
                case "sword":
                    return images[5];
                case "break":
                    return images[6];
                case "cursor":
                    return images[7];
                case "recovery":
                    return images[8];
                case "invis":
                    return images[9];
                case "object":
                    return images[10];
                case "null":
                    return images[11];
                default:
                    throw new ArgumentOutOfRangeException();

            }
        }

    }
}