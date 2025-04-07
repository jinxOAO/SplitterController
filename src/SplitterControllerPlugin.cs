using BepInEx;
using crecheng.DSPModSave;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SplitterController
{
    [BepInDependency(DSPModSavePlugin.MODGUID)]
    [BepInPlugin(GUID, NAME, VERSION)]
    public class SplitterControllerPlugin:BaseUnityPlugin, IModCanSave
    {
        public const string NAME = "SplitterController";
        public const string GUID = "com.GniMaerd.SplitterController";
        public const string VERSION = "0.1.0";
        public const int VERSIONINT = 100;

        public void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(UISplitterController));
            Harmony.CreateAndPatchAll(typeof(SplitterController));
        }

        public void Export(BinaryWriter w)
        {

            RuntimeData.Export(w);
        }

        public void Import(BinaryReader r)
        {
            UISplitterController.Init();
            RuntimeData.Import(r);
        }

        public void IntoOtherSave()
        {
            UISplitterController.Init();
            RuntimeData.InitWhenLoad();
        }
    }
}
