using Localisation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class LanguageManager : SingleInstance<LanguageManager>
{
    public override string Name { get; } = "Language Manager";

    public Action<string> OnLanguageChanged;

    private string currentLanguageName;
    private string lastLanguageName = "English";

    public ILanguage CurrentLanguage { get; private set; } = new English();
    Dictionary<string, ILanguage> Dic_Language = new Dictionary<string, ILanguage>
    {
        { "简体中文",new Chinese()},
        { "English",new English()},
    };

    void Awake()
    {
        OnLanguageChanged += ChangLanguage;
    }

    void Update()
    {
        currentLanguageName = LocalisationManager.Instance.currLangName;

        if (!lastLanguageName.Equals(currentLanguageName))
        {
            lastLanguageName = currentLanguageName;

            OnLanguageChanged.Invoke(currentLanguageName);
        }
    }

    void ChangLanguage(string value)
    {
        try
        {
            CurrentLanguage = Dic_Language[value];
        }
        catch
        {
            CurrentLanguage = Dic_Language["English"];
        }
    }


}

public interface ILanguage
{
    string UpKey { get; }
    string DownKey { get; }
    string BackKey { get; }
    string ClutchKey { get; }
    List<string> Model { get; }
    string Strength { get; }
    string Ratio { get; }
}


public class Chinese : ILanguage
{
    public string UpKey { get; } = "加挡";
    public string DownKey { get; } = "减挡";
    public string BackKey { get; } = "倒挡";
    public string ClutchKey { get; } = "离合";
    public List<string> Model { get; } = new List<string> { "速度模式", "角度模式" ,"变换模式"};
    public string Strength { get; } = "马力";
    public string Ratio { get; } = "变速比例";

}

public class English : ILanguage
{
    public string UpKey { get; } = "Add Gear";
    public string DownKey { get; } = "Reduce Gear";
    public string BackKey { get; } = "Reverse gear";
    public string ClutchKey { get; } = "Clutch";
    public List<string> Model { get; } = new List<string> { "Speed", "Angle", "Transform" };
    public string Strength { get; } = "HorsePower";
    public string Ratio { get; } = "Ratio";
}



