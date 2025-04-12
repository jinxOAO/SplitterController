using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace SplitterController
{
    public class SplitterController
    {
        /// <summary>
        /// 主要逻辑
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="sp"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), "UpdateSplitter")]
        public static bool UpdateSplitterPrefix(ref CargoTraffic __instance, ref SplitterComponent sp, long time)
        {
            sp.CheckPriorityPreset();
            if (sp.topId == 0)
            {
                if (sp.input0 == 0 || sp.output0 == 0)
                {
                    return false;
                }
            }
            else if (sp.input0 == 0 && sp.output0 == 0)
            {
                return false;
            }
            int planetId = __instance.factory.planetId;
            int splitterId = sp.id;
            if (!RuntimeData.ratios.ContainsKey(planetId))
                return true;
            if (!RuntimeData.ratios[planetId].ContainsKey(splitterId))
                return true;

            if (sp.outFilter > 0 || sp.outFilterPreset > 0 || sp.topId > 0)
                return true;
            if (!sp.inPriority || !sp.outPriority) // 必须设置了in和out的首选，才能有主路这个定义，才能执行特殊逻辑
                return true;
            if (!SelfLegal(ref sp))
                return true;

            //if (!SelfLegal(ref sp)) // 还是用上面写的
            //    return true;
            if (sp.input0 == 0 || sp.input1 == 0 || sp.output0 == 0)
                return true;

            if (RuntimeData.ratios.ContainsKey(planetId) && RuntimeData.ratios[planetId].ContainsKey(splitterId))
            {
                RatioSetting ratio = RuntimeData.ratios[planetId][splitterId];
                if (ratio.main <= 0 && ratio.side <= 0) // 全0则无效
                {
                    Dictionary<int, RatioSetting> obj = RuntimeData.ratios[planetId];
                    lock (obj)
                    {
                        RuntimeData.ratios[planetId].Remove(splitterId);
                    }
                    return true;
                }
                CargoPassingData cur = null;
                ratio = RuntimeData.ratios[planetId][splitterId];
                if (!RuntimeData.passingDatas.ContainsKey(planetId))
                    RuntimeData.passingDatas.AddOrUpdate(planetId, new ConcurrentDictionary<int, CargoPassingData>(), (x, y) => new ConcurrentDictionary<int, CargoPassingData>());
                if (!RuntimeData.passingDatas[planetId].ContainsKey(splitterId))
                {
                    CargoPassingData curCount = new CargoPassingData();
                    curCount.InitFrom(ratio);
                    RuntimeData.passingDatas[planetId].AddOrUpdate(splitterId, curCount, (x, y) => curCount);
                }
                cur = RuntimeData.passingDatas[planetId][splitterId];
                CargoPath fromPath = null;
                CargoPath overflowPath = null;
                int fromCargoIdx = -1;
                int overflowCargoIdx = -1;
                bool isFromMain = true;
                bool mainPathCargoIsSideCargo = false;
                // 用于记录上次的侧边输入的cargoItemId，是用来判断主路通过“非旁路”的货物数量的
                if (cur.lastSideItem <= 0) 
                {
                    CargoPath sidePath = null;
                    int sideCargoIdx = -1;
                    sidePath = __instance.GetCargoPath(__instance.beltPool[sp.input1].segPathId);
                    sideCargoIdx = sidePath.GetCargoIdAtRear();
                    if (sideCargoIdx != -1)
                        cur.lastSideItem = __instance.container.cargoPool[sideCargoIdx].item;
                }

                // 过度记录时，进行重置
                if(cur.main < -4)
                    cur.main = -4;
                if(cur.side < -4)
                    cur.side = -4;

            // 主要逻辑
            TakeLogic:
                // 尝试从主路取物品，看看物品是不是和side的输入一样
                fromPath = __instance.GetCargoPath(__instance.beltPool[sp.input0].segPathId);
                fromCargoIdx = fromPath.GetCargoIdAtRear();
                int mainPathItemId = -1;
                if (fromCargoIdx != -1)
                    mainPathItemId = __instance.container.cargoPool[fromCargoIdx].item;
                mainPathCargoIsSideCargo = mainPathItemId == cur.lastSideItem;
                if (cur.side > 0) // 尚有按比例的旁路物品待插入
                {
                    if (cur.main > 0 || mainPathCargoIsSideCargo) // 还有主路物品按比例未通过，或者主路上即将通过的物品与支路的物品相同（此时无视主路通过数，优先通过，且不计数主路，是否计数支路看情况）。
                    {
                        if (fromCargoIdx == -1) // 主路为空位，则将旁路物品插入
                        {
                            isFromMain = false;
                            fromPath = __instance.GetCargoPath(__instance.beltPool[sp.input1].segPathId);
                            fromCargoIdx = fromPath.GetCargoIdAtRear();
                        }
                    }
                    else // 主路物品通过数全部用尽，则查看旁路物品，有则优先旁路物品，且允许旁路物品挤掉主路物品
                    {
                        CargoPath tempSidePath = __instance.GetCargoPath(__instance.beltPool[sp.input1].segPathId);
                        int tempSideCargoIdx = tempSidePath.GetCargoIdAtRear();
                        if (tempSideCargoIdx != -1) // 如果支路物品确实有，那么用支路。否则面临：支路需要加入但是缺货的情况，这种情况下主路无视比例要求，强行通过，输出到主输出
                        {
                            isFromMain = false;
                            fromPath = __instance.GetCargoPath(__instance.beltPool[sp.input1].segPathId);
                            fromCargoIdx = fromPath.GetCargoIdAtRear();
                            // 并且，如果主路还有物品，也要取，然后准备输出到溢出口
                            overflowPath = __instance.GetCargoPath(__instance.beltPool[sp.input0].segPathId);
                            overflowCargoIdx = overflowPath.GetCargoIdAtRear();
                        }
                    }
                }
                else // 旁路货物通过数用尽
                {
                    if (cur.main > 0) // 如果主路尚有未用尽的通过数
                    {
                        // 从主路取物品
                        fromPath = __instance.GetCargoPath(__instance.beltPool[sp.input0].segPathId);
                        fromCargoIdx = fromPath.GetCargoIdAtRear();
                    }
                    else // 所有路的通过数都用尽，进行重置
                    {
                        if (cur.AddUntilPositive(ratio))
                            goto TakeLogic;
                        else
                            return false;
                    }
                }
                if (fromCargoIdx != -1) // 如果取到了货物
                {
                    int stack = __instance.container.cargoPool[fromCargoIdx].stack;
                    if (!isFromMain) // 如果从旁路取到了物品，更新旁路物品Id
                        cur.lastSideItem = __instance.container.cargoPool[fromCargoIdx].item;
                    CargoPath toPath = __instance.GetCargoPath(__instance.beltPool[sp.output0].segPathId);
                    int headIndex = toPath.TestBlankAtHead();
                    if (toPath != null && toPath.pathLength > 10 && headIndex >= 0)
                    {
                        int cargoId = fromPath.TryPickCargoAtEnd();
                        if (cargoId >= 0)
                        {
                            toPath.InsertCargoAtHeadDirect(cargoId, headIndex);
                            if (isFromMain && !mainPathCargoIsSideCargo)
                                cur.main -= stack;
                            else if (!isFromMain || (isFromMain && mainPathCargoIsSideCargo))
                                cur.side -= stack;
                        }
                        else
                        {
                            Debug.LogError("取到了非法货物");
                        }
                    }
                }

                if (overflowCargoIdx != -1 && sp.output1 != 0) // 如果有货物需要溢出，且有溢出口
                {
                    int stack = __instance.container.cargoPool[overflowCargoIdx].stack;
                    CargoPath toPath = __instance.GetCargoPath(__instance.beltPool[sp.output1].segPathId);
                    int headIndex = toPath.TestBlankAtHead();
                    if (toPath != null && toPath.pathLength > 10 && headIndex >= 0)
                    {
                        int cargoId = overflowPath.TryPickCargoAtEnd();
                        if (cargoId >= 0)
                        {
                            toPath.InsertCargoAtHeadDirect(cargoId, headIndex);
                            // 溢出物不需要记录cur
                        }
                        else
                        {
                            Debug.LogError("取到了非法货物");
                        }
                    }
                }
            }
            return false;
        }


        public static void RemoveAllFilterSettings(int planetId, int splitterId, bool exceptOutputPriority = true)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetByAstroId(planetId).factory;
            CargoTraffic traffic = factory.cargoTraffic;
            if (splitterId == 0 || factory == null)
            {
                return;
            }
            SplitterComponent splitterComponent = traffic.splitterPool[splitterId];
            if (splitterComponent.id != splitterId)
            {
                return;
            }
            traffic.splitterPool[splitterId].outFilter = 0;
            traffic.splitterPool[splitterId].outFilterPreset = 0;
            //bool flag;
            //int num;
            //int num2;
            //factory.ReadObjectConn(splitterComponent.entityId, 0, out flag, out num, out num2);
            //bool flag2;
            //int num3;
            //factory.ReadObjectConn(splitterComponent.entityId, 1, out flag2, out num3, out num2);
            //bool flag3;
            //int num4;
            //factory.ReadObjectConn(splitterComponent.entityId, 2, out flag3, out num4, out num2);
            //bool flag4;
            //int num5;
            //factory.ReadObjectConn(splitterComponent.entityId, 3, out flag4, out num5, out num2);
        }

        /// <summary>
        /// 判断本身是否支持按设定的比例执行逻辑
        /// 如果有filter，或者上边有箱子，只能执行原逻辑。
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        public static bool SelfLegal(ref SplitterComponent sp)
        {
            // 注意！！！！！！！！！！！！如果要修改，记得也要修改上面UpdateSplitterPrefix里面对应的判据
            return sp.topId <= 0 && sp.outFilter <= 0 && sp.outFilterPreset <= 0 && sp.inPriority && sp.outPriority && sp.input2 <= 0 && sp.output2 <= 0;
        }
    }
}
