using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UITools;
using UnityEngine;
using UnityEngine.UI;

namespace SplitterController
{
    public static class UISplitterController
    {
        public static bool inited = false;
        public static InputField beltAInputField;
        public static InputField beltBInputField;
        public static InputField beltCInputField;
        public static InputField beltDInputField;
        public static RectTransform beltAInputFieldRT;
        public static RectTransform beltBInputFieldRT;
        public static RectTransform beltCInputFieldRT;
        public static RectTransform beltDInputFieldRT;
        public const float InputFieldXPos = 30;
        public const float InputFieldYPos = -20;
        public const float InputFieldHeight = 30;
        public const float InputFieldWidth = 45;
        public static void Init()
        {
            if (inited)
                return;

            inited = true;

            GameObject oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/Scroll View/Viewport/Content/group-1/input-short-text"); if (oriInputFieldObj == null)
                oriInputFieldObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Blueprint Browser/inspector-group/BP-panel-scroll(Clone)/Viewport/pane/group-1/input-short-text");
            if (oriInputFieldObj == null)
            {
                Debug.LogError("Error when init oriInputField because some other mods has changed the Blueprint Browser UI. Please check if you've install the BluePrintTweaks and then contant jinxOAO.");
                return;
            }
            GameObject parentObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Splitter Window/splitter-desc/splitter-circle");

            GameObject inputFieldAObj = GameObject.Instantiate(oriInputFieldObj, parentObj.transform);
            inputFieldAObj.name = "ratio-input-A";
            inputFieldAObj.transform.localPosition = new Vector3(120, 0, 0);
            inputFieldAObj.GetComponent<RectTransform>().sizeDelta = new Vector2( InputFieldWidth, InputFieldHeight);
            inputFieldAObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            inputFieldAObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
            inputFieldAObj.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            inputFieldAObj.GetComponent<InputField>().text = "0";
            inputFieldAObj.GetComponent<InputField>().contentType = InputField.ContentType.DecimalNumber;
            inputFieldAObj.GetComponent<InputField>().characterLimit = 5;
            inputFieldAObj.GetComponent<InputField>().transition = Selectable.Transition.None; // 要不然鼠标不在上面时颜色会很浅，刚打开容易找不到，不够明显
            inputFieldAObj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
            inputFieldAObj.transform.Find("value-text").GetComponent<Text>().color = Color.white;
            inputFieldAObj.transform.Find("value-text").GetComponent<Text>().fontSize = 14;
            inputFieldAObj.GetComponent<UIButton>().tips.tipTitle = "";
            inputFieldAObj.GetComponent<UIButton>().tips.tipText = "";

            beltAInputField = inputFieldAObj.GetComponent<InputField>();
            beltAInputFieldRT = inputFieldAObj.GetComponent<RectTransform>();
            inputFieldAObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            inputFieldAObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnRatioChange(0));
            inputFieldAObj.SetActive(false);
            inputFieldAObj.SetActive(true); // 这样切一次颜色才能显示正常

            GameObject inputFieldBObj = GameObject.Instantiate(inputFieldAObj, parentObj.transform);
            inputFieldBObj.name = "ratio-input-B";
            beltBInputField = inputFieldBObj.GetComponent<InputField>();
            beltBInputFieldRT = inputFieldBObj.GetComponent<RectTransform>();
            inputFieldBObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            inputFieldBObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnRatioChange(1));
            inputFieldBObj.SetActive(false);
            inputFieldBObj.SetActive(true); // 这样切一次颜色才能显示正常

            GameObject inputFieldCObj = GameObject.Instantiate(inputFieldAObj, parentObj.transform);
            inputFieldCObj.name = "ratio-input-C";
            beltCInputField = inputFieldCObj.GetComponent<InputField>();
            beltCInputFieldRT = inputFieldCObj.GetComponent<RectTransform>();
            inputFieldCObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            inputFieldCObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnRatioChange(2));
            inputFieldCObj.SetActive(false);
            inputFieldCObj.SetActive(true); // 这样切一次颜色才能显示正常

            GameObject inputFieldDObj = GameObject.Instantiate(inputFieldAObj, parentObj.transform);
            inputFieldDObj.name = "ratio-input-D";
            beltDInputField = inputFieldDObj.GetComponent<InputField>();
            beltDInputFieldRT = inputFieldDObj.GetComponent<RectTransform>();
            inputFieldDObj.GetComponent<InputField>().onEndEdit.RemoveAllListeners();
            inputFieldDObj.GetComponent<InputField>().onEndEdit.AddListener((x) => OnRatioChange(3));
            inputFieldDObj.SetActive(false);
            inputFieldDObj.SetActive(true); // 这样切一次颜色才能显示正常
        }

        public static void OnRatioChange(int index)
        {
            UISplitterWindow splitterWindow = UIRoot.instance?.uiGame?.splitterWindow;
            if (splitterWindow != null)
            {
                int planetId = splitterWindow.factory.planetId;
                PlanetFactory factory = GameMain.galaxy.PlanetById(planetId).factory;
                int splitterId = splitterWindow.splitterId; // 是factory.entityPool[?].splitterId
                if (splitterId == 0 || factory == null)
                {
                    return;
                }
                CargoTraffic traffic = factory.cargoTraffic;
                SplitterComponent splitterComponent = traffic.splitterPool[splitterId];
                if (splitterComponent.id != splitterId)
                {
                    return;
                }
                if (!RuntimeData.ratios.ContainsKey(planetId))
                    RuntimeData.ratios[planetId] = new Dictionary<int, RatioData>();
                if (!RuntimeData.ratios[planetId].ContainsKey(splitterId))
                    RuntimeData.ratios[planetId][splitterId] = new RatioData();
                if (index == 0)
                {
                    int target = 0;
                    try
                    {
                        target = (int)Convert.ToDouble(beltAInputField.text);
                    }
                    catch (Exception)
                    {
                        target = 0;
                    }
                    if (splitterComponent.input0 == splitterComponent.beltA)
                        RuntimeData.ratios[planetId][splitterId].main = target > 0 ? target : 0;
                    else if (splitterComponent.input1 == splitterComponent.beltA)
                        RuntimeData.ratios[planetId][splitterId].side = target > 0 ? target : 0;
                    else
                        Debug.LogWarning($"四项分流器设置失败！");

                }
                if (index == 1)
                {
                    int target = 0;
                    try
                    {
                        target = (int)Convert.ToDouble(beltBInputField.text);
                    }
                    catch (Exception)
                    {
                        target = 0;
                    }
                    if (splitterComponent.input0 == splitterComponent.beltB)
                        RuntimeData.ratios[planetId][splitterId].main = target > 0 ? target : 0;
                    else if (splitterComponent.input1 == splitterComponent.beltB)
                        RuntimeData.ratios[planetId][splitterId].side = target > 0 ? target : 0;
                    else
                        Debug.LogWarning($"四项分流器设置失败！");
                }
                if (index == 2)
                {
                    int target = 0;
                    try
                    {
                        target = (int)Convert.ToDouble(beltCInputField.text);
                    }
                    catch (Exception)
                    {
                        target = 0;
                    }
                    if (splitterComponent.input0 == splitterComponent.beltC)
                        RuntimeData.ratios[planetId][splitterId].main = target > 0 ? target : 0;
                    else if (splitterComponent.input1 == splitterComponent.beltC)
                        RuntimeData.ratios[planetId][splitterId].side = target > 0 ? target : 0;
                    else
                        Debug.LogWarning($"四项分流器设置失败！");
                }
                if (index == 3)
                {
                    int target = 0;
                    try
                    {
                        target = (int)Convert.ToDouble(beltDInputField.text);
                    }
                    catch (Exception)
                    {
                        target = 0;
                    }
                    if (splitterComponent.input0 == splitterComponent.beltD)
                        RuntimeData.ratios[planetId][splitterId].main = target > 0 ? target : 0;
                    else if (splitterComponent.input1 == splitterComponent.beltD)
                        RuntimeData.ratios[planetId][splitterId].side = target > 0 ? target : 0;
                    else
                        Debug.LogWarning($"四项分流器设置失败！");
                }

                // 设置任何比例项时，移除所有filter设定和输入优先级设定
                SplitterController.RemoveAllFilterSettings(planetId, splitterId);
                RefreshInputFields();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISplitterWindow), "_OnOpen")]
        [HarmonyPatch(typeof(UISplitterWindow), "OnCircleClick")]
        public static void RefreshInputFields()
        {
            UISplitterWindow splitterWindow = UIRoot.instance?.uiGame?.splitterWindow;
            if (splitterWindow != null)
            {
                int planetId = splitterWindow.factory.planetId;
                int splitterId = splitterWindow.splitterId; // 是factory.entityPool[?].splitterId
                if (RuntimeData.ratios.ContainsKey(planetId))
                {
                    if (RuntimeData.ratios[planetId].ContainsKey(splitterId))
                    {
                        PlanetFactory factory = GameMain.galaxy.PlanetById(planetId).factory;
                        if (splitterId == 0 || factory == null)
                        {
                            goto Reset;
                        }
                        CargoTraffic traffic = factory.cargoTraffic;
                        SplitterComponent splitterComponent = traffic.splitterPool[splitterId];
                        if (splitterComponent.id != splitterId)
                        {
                            goto Reset;
                        }
                        if (splitterComponent.input0 == splitterComponent.beltA)
                            beltAInputField.text = RuntimeData.ratios[planetId][splitterId].main.ToString();
                        else if (splitterComponent.input0 == splitterComponent.beltB)
                            beltBInputField.text = RuntimeData.ratios[planetId][splitterId].main.ToString();
                        else if (splitterComponent.input0 == splitterComponent.beltC)
                            beltCInputField.text = RuntimeData.ratios[planetId][splitterId].main.ToString();
                        else if (splitterComponent.input0 == splitterComponent.beltD)
                            beltDInputField.text = RuntimeData.ratios[planetId][splitterId].main.ToString();


                        if (splitterComponent.input1 == splitterComponent.beltA)
                            beltAInputField.text = RuntimeData.ratios[planetId][splitterId].side.ToString();
                        else if (splitterComponent.input1 == splitterComponent.beltB)
                            beltBInputField.text = RuntimeData.ratios[planetId][splitterId].side.ToString();
                        else if (splitterComponent.input1 == splitterComponent.beltC)
                            beltCInputField.text = RuntimeData.ratios[planetId][splitterId].side.ToString();
                        else if (splitterComponent.input1 == splitterComponent.beltD)
                            beltDInputField.text = RuntimeData.ratios[planetId][splitterId].side.ToString();

                        return;
                    }
                }
            }

            Reset:
            beltAInputField.text = "0";
            beltBInputField.text = "0";
            beltCInputField.text = "0";
            beltDInputField.text = "0";
        }
        //UIRoot.instance.uiGame.splitterWindow;

        /// <summary>
        /// 用于让所有输入框跟随
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISplitterWindow), "_OnUpdate")]
        public static void UISplitterWindowOnUpdatPostfix(UISplitterWindow __instance)
        {
            if (__instance.splitterId == 0 || __instance.factory == null)
            {
                return;
            }
            SplitterComponent splitterComponent = __instance.traffic.splitterPool[__instance.splitterId];
            if (splitterComponent.id != __instance.splitterId)
            {
                return;
            }
            bool isOutput0;
            __instance.factory.ReadObjectConn(splitterComponent.entityId, 0, out isOutput0, out _, out _);
            bool isOutput1;
            __instance.factory.ReadObjectConn(splitterComponent.entityId, 1, out isOutput1, out _, out _);
            bool isOutput2;
            __instance.factory.ReadObjectConn(splitterComponent.entityId, 2, out isOutput2, out _, out _);
            bool isOutput3;
            __instance.factory.ReadObjectConn(splitterComponent.entityId, 3, out isOutput3, out _, out _);

            bool selfLegal = SplitterController.SelfLegal(ref splitterComponent);
            if (__instance.circleA.activeSelf && !isOutput0 && selfLegal) 
            {
                beltAInputFieldRT.gameObject.SetActive(true);
                Vector3 circlePos = __instance.circleA.GetComponent<RectTransform>().anchoredPosition3D;
                beltAInputFieldRT.anchoredPosition3D = new Vector3(circlePos.x + InputFieldXPos, circlePos.y + InputFieldYPos, circlePos.z);
            }
            else
            {
                beltAInputFieldRT.gameObject.SetActive(false);
            }
            if (__instance.circleB.activeSelf && !isOutput1 && selfLegal)
            {
                beltBInputFieldRT.gameObject.SetActive(true);
                Vector3 circlePos = __instance.circleB.GetComponent<RectTransform>().anchoredPosition3D;
                beltBInputFieldRT.anchoredPosition3D = new Vector3(circlePos.x + InputFieldXPos, circlePos.y + InputFieldYPos, circlePos.z);
            }
            else
            {
                beltBInputFieldRT.gameObject.SetActive(false);
            }
            if (__instance.circleC.activeSelf && !isOutput2 && selfLegal)
            {
                beltCInputFieldRT.gameObject.SetActive(true);
                Vector3 circlePos = __instance.circleC.GetComponent<RectTransform>().anchoredPosition3D;
                beltCInputFieldRT.anchoredPosition3D = new Vector3(circlePos.x + InputFieldXPos, circlePos.y + InputFieldYPos, circlePos.z);
            }
            else
            {
                beltCInputFieldRT.gameObject.SetActive(false);
            }
            if (__instance.circleD.activeSelf && !isOutput3 && selfLegal)
            {
                beltDInputFieldRT.gameObject.SetActive(true);
                Vector3 circlePos = __instance.circleD.GetComponent<RectTransform>().anchoredPosition3D;
                beltDInputFieldRT.anchoredPosition3D = new Vector3(circlePos.x + InputFieldXPos, circlePos.y + InputFieldYPos, circlePos.z);
            }
            else
            {
                beltDInputFieldRT.gameObject.SetActive(false);
            }

            if(!selfLegal)
                RemoveRatioSettings();
        }


        public static void RemoveRatioSettings()
        {
            UISplitterWindow splitterWindow = UIRoot.instance?.uiGame?.splitterWindow;
            if (splitterWindow != null)
            {
                int planetId = splitterWindow.factory.planetId;
                int splitterId = splitterWindow.splitterId; // 是factory.entityPool[?].splitterId
                if (RuntimeData.ratios.ContainsKey(planetId))
                {
                    if (RuntimeData.ratios[planetId].ContainsKey(splitterId))
                    {
                        RuntimeData.ratios[planetId].Remove(splitterId);
                        RefreshInputFields();
                    }
                }
            }
        }
    }
}
