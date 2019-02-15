using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;


    public class Modloader : ModEntryPoint
    {
        public GameObject Mod;

        public override void OnLoad()
        {
            Mod = new GameObject("Transmission Block Mod");
            LanguageManager.Instance.transform.SetParent(Mod.transform);
            GameObject.DontDestroyOnLoad(Mod);
        }
    }
