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
        public static Dictionary<int, Dictionary<int, RatioSetting>> ratios; // ratios[planetId][splitterId]
        public static ConcurrentDictionary<int, ConcurrentDictionary<int, CargoPassingData>> passingDatas; // 尚未满足数量的货物们的计数

        public static void InitWhenLoad()
        {
            ratios = new Dictionary<int, Dictionary<int, RatioSetting>>();
            passingDatas = new ConcurrentDictionary<int, ConcurrentDictionary<int, CargoPassingData>>();
            if(GameMain.data.factories != null)
            {
                for (int i = 0; i < GameMain.data.factoryCount; i++)
                {
                    if (GameMain.data.factories[i]!=null)
                    {
                        int planetId = GameMain.data.factories[i].planetId;
                        ratios[planetId] = new Dictionary<int, RatioSetting>();
                        passingDatas.AddOrUpdate(planetId, new ConcurrentDictionary<int, CargoPassingData>(), (x, y) => new ConcurrentDictionary<int, CargoPassingData>());
                    }
                }
            }
        }

        public static void Import(BinaryReader r)
        {
            RuntimeData.InitWhenLoad();
            int ratioCount = r.ReadInt32();
            for (int i = 0; i < ratioCount; i++)
            {
                int key = r.ReadInt32();
                int count = r.ReadInt32();
                Dictionary<int, RatioSetting> planetData = new Dictionary<int, RatioSetting>();
                for (int j = 0; j < count; j++)
                {
                    int key2 = r.ReadInt32();
                    RatioSetting rs = new RatioSetting();
                    rs.main = r.ReadInt32();
                    rs.side = r.ReadInt32();
                    rs.setting = r.ReadInt32();
                    planetData[key2] = rs;
                }
                ratios[key] = planetData;
            }
            int awaitDataCount = r.ReadInt32();
            for (int i = 0; i < awaitDataCount; i++)
            {
                int key = r.ReadInt32();
                int count = r.ReadInt32();
                ConcurrentDictionary<int, CargoPassingData> planetData = new ConcurrentDictionary<int, CargoPassingData>();
                for (int j = 0; j < count; j++)
                {
                    int key2 = r.ReadInt32();
                    CargoPassingData cpd = new CargoPassingData();
                    cpd.main = r.ReadInt32();
                    cpd.side = r.ReadInt32();
                    cpd.lastSideItem = r.ReadInt32();
                    planetData.AddOrUpdate(key2, cpd, (x, y) => cpd);
                }
                passingDatas.AddOrUpdate(key, planetData, (x, y) => planetData);
            }
        }

        public static void Export(BinaryWriter w)
        {
            w.Write(ratios.Count);
            foreach (var ratioKV in ratios)
            {
                w.Write(ratioKV.Key);
                w.Write(ratioKV.Value.Count);
                foreach(var pair in ratioKV.Value)
                {
                    w.Write(pair.Key);
                    w.Write(pair.Value.main);
                    w.Write(pair.Value.side);
                    w.Write(pair.Value.setting);
                }
            }
            w.Write(passingDatas.Count);
            foreach (var awaitKV in passingDatas)
            {
                w.Write(awaitKV.Key);
                w.Write(awaitKV.Value.Count);
                foreach (var pair in awaitKV.Value)
                {
                    w.Write(pair.Key);
                    w.Write(pair.Value.main);
                    w.Write(pair.Value.side);
                    w.Write(pair.Value.lastSideItem);
                }
            }
        }
    }

    public class RatioSetting
    {
        public int main;
        public int side;
        public int setting;
        public RatioSetting()
        {
            main = 0;
            side = 0;
            setting = 0;
        }
    }

    public class CargoPassingData
    {
        public int main;
        public int side;
        public int lastSideItem;
        public CargoPassingData()
        {
            main = 0;
            side = 0;
            lastSideItem = 0;
        }

        public void InitFrom(RatioSetting ratio)
        {
            main = ratio.main;
            side = ratio.side;
        }

        public void AddFrom(RatioSetting ratio)
        {
            main += ratio.main;
            side += ratio.side;
        }

        public bool AddUntilPositive(RatioSetting ratio)
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
