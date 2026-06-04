using PolylinerNet;
using System.Text;

namespace StravaStats.CustomPolyliner
{
    public class ValhallaPolyliner : PolylinerBase
    {
        public string Encode(List<PolylinePoint> polylinePoints)
        {
            StringBuilder stringBuilder = new StringBuilder();
            long num = 0L;
            long num2 = 0L;
            foreach (PolylinePoint polylinePoint in polylinePoints)
            {
                long num3 = (long)(polylinePoint.Latitude * 1000000.0);
                long num4 = (long)(polylinePoint.Longitude * 1000000.0);
                EncodeNextCoordinate(num3 - num, stringBuilder);
                EncodeNextCoordinate(num4 - num2, stringBuilder);
                num = num3;
                num2 = num4;
            }

            return stringBuilder.ToString();
        }

        public List<PolylinePoint> Decode(string polyline)
        {
            List<PolylinePoint> list = new List<PolylinePoint>(polyline.Length / 2);
            int polylineIndex = 0;
            int num = 0;
            int num2 = 0;
            while (polylineIndex < polyline.Length)
            {
                num += DecodeNextCoordinate(polyline, ref polylineIndex);
                num2 += DecodeNextCoordinate(polyline, ref polylineIndex);
                list.Add(new PolylinePoint((double)num * 1E-06, (double)num2 * 1E-06));
            }

            return list;
        }
    }
}
