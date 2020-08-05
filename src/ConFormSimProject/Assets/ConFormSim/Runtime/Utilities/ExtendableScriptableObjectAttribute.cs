using UnityEngine;
using System.Collections.Generic;

public class ExtendableScriptableObjectAttribute : PropertyAttribute
{
    public bool showType;
    public bool disableType;
    public bool allowCreateButton;
    public string[] fieldsToUseCustomListEditor = new string[0];
    public bool customListShowSize;
    public bool customListAlwaysExtended;

    public ExtendableScriptableObjectAttribute(
        bool showType=true, 
        bool disableType=false, 
        bool allowCreateButton=false,
        string[] fieldsToUseCustomListEditor=null,
        bool customListShowSize=true,
        bool customListAlwaysExtended=false)
    {
        this.showType = showType;
        this.disableType = disableType;
        this.allowCreateButton = allowCreateButton;
        if (fieldsToUseCustomListEditor != null)
            this.fieldsToUseCustomListEditor = fieldsToUseCustomListEditor;
        this.customListShowSize = customListShowSize;
        this.customListAlwaysExtended = customListAlwaysExtended;
    }
}