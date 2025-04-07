using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplitterController
{
    public static class RuntimeData
    {
        public static Dictionary<int, Dictionary<int, RatioData>> ratios; // ratios[planetId][splitterId]
        public static ConcurrentDictionary<int, ConcurrentDictionary<int, RatioData>> cargosAwait; // 尚未满足数量的货物们的计数

        public static void InitWhenLoad()
        {
            ratios = new Dictionary<int, Dictionary<int, RatioData>>();
            cargosAwait = new ConcurrentDictionary<int, ConcurrentDictionary<int, RatioData>>();
            if(GameMain.data.factories != null)
            {
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    if (GameMain.data.factories[i]!=null)
                    {
                        int planetId = GameMain.data.factories[i].planetId;
                        ratios[planetId] = new Dictionary<int, RatioData>();
                        cargosAwait.AddOrUpdate(planetId, new ConcurrentDictionary<int, RatioData>(), (x, y) => new ConcurrentDictionary<int, RatioData>());
                    }
                }
            }
        }

        public static void Import(BinaryReader r)
        {
            RuntimeData.InitWhenLoad();
        }

        public static void Export(BinaryWriter w)
        {

        }
    }

    public class RatioData
    {
        public int main;
        public int side;
        public RatioData()
        {
            main = 0;
            side = 0;
        }

        public void InitFrom(RatioData ratio)
        {
            main = ratio.main;
            side = ratio.side;
        }

        public void AddFrom(RatioData ratio)
        {
            main += ratio.main;
            side += ratio.side;
        }

        public bool AddUntilPositive(RatioData ratio)
        {
            if (ratio.main <= 0 && ratio.side <= 0)
                return false;
            while(true)
            {
                main += ratio.main;
                side += ratio.side;

                if (main > 0 || side > 0)
                    break;
            }
            return true;
        }
    }
}
