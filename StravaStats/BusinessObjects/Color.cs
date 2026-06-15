using MudBlazor.Utilities;

namespace StravaStats.BusinessObjects;

public class Color
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }

    public Color(byte r, byte g, byte b)
    {
        R = r;
        G = g;
        B = b;
    }
    public Color(float r, float g, float b)
    {
        R = (byte)(r * 255);
        G = (byte)(g * 255);
        B = (byte)(b * 255);
    }
    public Color()
    {
        R = 0;
        G = 0;
        B = 0;
    }

    public static Color operator *(Color rgb, float factor)
    {
        float r = (rgb.R * factor);
        float g = (rgb.G * factor);
        float b = (rgb.B * factor);

        // when brightness goes over max values the light turns more whiteish
        float overflow = Math.Max(0, r - 255) + Math.Max(0, g - 255) + Math.Max(0, b - 255);
        r += overflow;
        g += overflow;
        b += overflow;

        r = Math.Min(r, 255);
        g = Math.Min(g, 255);
        b = Math.Min(b, 255);

        return new Color((byte)r, (byte)g, (byte)b);
    }

    public static Color operator *(Color rgb, double factor)
    {
        double r = (rgb.R * factor);
        double g = (rgb.G * factor);
        double b = (rgb.B * factor);

        // when brightness goes over max values the light turns more whiteish
        double overflow = Math.Max(0, r - 255) + Math.Max(0, g - 255) + Math.Max(0, b - 255);
        r += overflow;
        g += overflow;
        b += overflow;

        r = Math.Min(r, 255);
        g = Math.Min(g, 255);
        b = Math.Min(b, 255);

        return new Color((byte)r, (byte)g, (byte)b);
    }

    public Color Interpolate(Color other, float factor)
    {
        byte r = (byte)(R + (other.R - R) * factor);
        byte g = (byte)(G + (other.G - G) * factor);
        byte b = (byte)(B + (other.B - B) * factor);
        return new Color(r, g, b);
    }

    public Color InterpolateSqrt(Color other, float factor)
    {
        byte r = (byte)(Math.Sqrt(R * R + (other.R * other.R - R * R) * factor));
        byte g = (byte)(Math.Sqrt(G * G + (other.G * other.G - G * G) * factor));
        byte b = (byte)(Math.Sqrt(B * B + (other.B * other.B - B * B) * factor));
        return new Color(r, g, b);
    }

    public void WriteToBuffer(byte[] buffer, int position)
    {
        buffer[position] = R;
        buffer[position + 1] = G;
        buffer[position + 2] = B;
    }

    public static Color FromMudColor(MudColor mudColor)
    {
        return new Color(mudColor.R, mudColor.G, mudColor.B);
    }

    public static Color FromHsv(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return new Color
        {
            R = (byte)((r + m) * 255),
            G = (byte)((g + m) * 255),
            B = (byte)((b + m) * 255)
        };
    }

    public MudColor ToMudColor()
    {
        return new MudColor(R, G, B, (byte)255);
    }

    public string ToHex()
    {
        return $"#{Convert.ToHexString([R, G, B])}";
    }

    public Color Clone()
    {
        return new Color(R, G, B);
    }
}
