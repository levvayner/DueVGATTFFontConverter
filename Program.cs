using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System.Reflection;
using SixLabors.ImageSharp.Memory;
// See https://aka.ms/new-console-template for more information
Console.WriteLine("Due VGA Font Generator");

int fontSize = 12;
int paramCount = 0;
//bool flag = false;
string outputFileName = "Chars.h";
bool generateBmp = false;
string fontName = "Fonts/UbuntuSans.ttf";
if (args.Length > 0)
{
    for(int idx=0;idx < args.Length; idx++)
    //foreach(var arg in args)
    {
        var arg = args[idx];
        String currentArg = arg;
        if (arg.StartsWith('-'))
        {
            //flag = true;
            currentArg = arg[1..];
            if (currentArg == "font")
            {
                fontName = args[++idx];
            }
            else if (currentArg == "size")
            {
                int.TryParse(args[++idx], out fontSize);
            }
            else if (currentArg == "header")
            {
                outputFileName = args[++idx];
            }
            else if(currentArg == "bmp")
            {
                bool.TryParse(args[++idx], out generateBmp);
            }
        }
        else
        {
            if(arg.ToLower() == "help")
            {
                Console.WriteLine("use with flags: ");
                Console.WriteLine("\t-font [font-file-name.ttf]");
                Console.WriteLine("\t-size [font-size]");
                Console.WriteLine("\t-header [header-filename.h]");
                Console.WriteLine("\t-bmp [0,1]");
                Console.WriteLine("or specify all parameters");
                Console.WriteLine($"\te.g. {Assembly.GetExecutingAssembly().GetName()} 12 UbuntuSansMono.ttf Chars.h to create a header file for size 12 font using UbuntuSansMono.ttf into a file named Chars.h");
                return;
            }
            if (paramCount == 0)
            {
                //font
                fontName = args[idx];
            }
            else if (paramCount == 1)
            {
                //size
                int.TryParse(args[++idx], out fontSize);
            }
            else if (paramCount == 2)
            {
                //output file name
                outputFileName = args[idx];
            }
            else
            {
                Console.WriteLine("Too many parameters. Quitting!");
                return;
            }
            paramCount++;

        }
        
    }
}

FontCollection collection = new FontCollection();
//collection.Add("KaTeX_SansSerif-Regular.ttf");
collection.Add(fontName);
Font font = collection.Families.FirstOrDefault().CreateFont(fontSize);
RichTextOptions options = new RichTextOptions(font);
options.ColorFontSupport = ColorFontSupport.None;
//byte[] fontData = new byte[12 * 8];
Brush textBrush = Brushes.Solid(Color.White);
Pen pen = Pens.Solid(textBrush);
var maxCol = 0;
int stride = (int)Math.Ceiling((decimal)fontSize / 8); //8 bits per byte
List<byte[,]> data = new List<byte[,]>();
List<byte[]> chars = new List<byte[]>();
using (var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(fontSize, fontSize))
{

    image.Mutate(imageContext =>
    {

        for (int idx = 32; idx < 127; idx++)
        {
            imageContext.DrawText(new String((char)idx, 1), font, textBrush, new PointF(0, 0));
            if (generateBmp)
                try
                {
                    string charFN = ((char)idx == '.') ? "__" : ((char)idx).ToString();
                    image.SaveAsBmp($"Generated-{charFN}.bmp");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to generate bitmap for char {(char)idx}. Error: {ex.Message}");
                }

            byte[] pixelData = new byte[fontSize * fontSize * 4];
            byte[,] generatedData = new byte[fontSize, fontSize];
            Span<byte> letterPixels = new Span<byte>(pixelData, 0, fontSize * fontSize * 4);
            image.CopyPixelDataTo(letterPixels);


            //Console.WriteLine($"Writing char {(char)idx}");
            for (int pixelIdx = 0; pixelIdx < fontSize * fontSize; pixelIdx++)
            {
                int col = pixelIdx % fontSize;
                int row = pixelIdx / fontSize;

                if (letterPixels[pixelIdx * 4 + 0] != 0 ||
                    letterPixels[pixelIdx * 4 + 1] != 0 ||
                    letterPixels[pixelIdx * 4 + 2] != 0
                //letterPixels[pixelIdx * 4 + 3] != 0 //don't care about alpha if all color channels are 0
                )
                {
                    //pixel is present
                    generatedData[col, row] = 1;
                    if (maxCol < col)
                        maxCol = col;


                }
            }

            data.Add(generatedData);

            generatedData = new byte[fontSize, fontSize];
            imageContext.Clear(Color.Black);

        }
    });
}

//update stride
stride = (int)Math.Ceiling((decimal)(maxCol+1) / 8);
//compress generated data to bits
for (int charIdx = 32; charIdx < 127; charIdx++)
{
    byte[] outputData = new byte[stride * fontSize];
    for (int pixelIdx = 0; pixelIdx < fontSize * fontSize; pixelIdx++)
    {
        int col = pixelIdx % fontSize;
        int row = pixelIdx / fontSize;

        if (col > maxCol) continue;

        int curStrideByte = col / 8;
        int curStrideBit = pixelIdx % fontSize % 8;
        if (data[charIdx - 32][col, row] != 0)
            outputData[curStrideByte + (row * stride)] |= (byte)(1 << curStrideBit);
    }
    chars.Add(outputData);
}

//output char data to header file
using (StreamWriter writer = new StreamWriter(new FileStream(outputFileName, FileMode.Create)))
{
    writer.WriteLine("#ifndef _CHARS_H_");
    writer.WriteLine("#define _CHARS_H_");
    //writer.WriteLine($"const byte BYTES_PER_COLUMN = {stride};");
    //writer.WriteLine($"const byte BYTES_PER_ROW = {maxCol / 8};");
    writer.WriteLine($"const byte CHAR_WIDTH = {maxCol};");
    writer.WriteLine($"const byte CHAR_HEIGHT = {fontSize};");
    //ascii chars with offset of 32, starting with space

    writer.WriteLine($"const byte CHARS[{127 - 32}][{stride * fontSize}] =" + "{");
    int idx = 32;
    foreach (var cdata in chars)
    {
        writer.Write("\t{");
        foreach (var bdata in cdata)
        {
            writer.Write($"0x{bdata.ToString("X2")}, ");
        }

        writer.Write("}, // ");
        var descString = idx == 0x5C ? $"\"{(char)idx++} \"" : ((char)idx++).ToString();
        writer.WriteLine(descString);
    }

    writer.WriteLine("};");
    writer.WriteLine("#endif");

}

Console.WriteLine($"Generated {outputFileName} with font sourced from {fontName} in {maxCol}x{fontSize} bytes");



