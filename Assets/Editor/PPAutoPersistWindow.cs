//AlmostLogical Software - http://www.almostlogical.com - support@almostlogical.com
using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

public class PPAutoPersistWindow : EditorWindow
{
    static bool SHOW_ADDED_ALL = true; //turn this to true, to show add all button (added every component has significant editor performance overhead and may not achieve your desired result)

    string autoPersistSearch = "";
    Vector2 scrollPos;
    bool isShowOnlyEnabled = false;

    private static List<PPAutoPersistObject> defaultTypes;

    [MenuItem("Window/PlayModePersist Auto Persist Settings")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        PPAutoPersistWindow window = (PPAutoPersistWindow)EditorWindow.GetWindow(typeof(PPAutoPersistWindow),true,"Auto Persist");
        window.minSize = new Vector2(325,300);
        window.maxSize = new Vector2(325,800);
    }

    public void OnEnable()
    {
        //foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
        Assembly asm;
        
        if (defaultTypes == null)
        {
            defaultTypes = new List<PPAutoPersistObject>();
            asm = typeof(Component).Assembly;
            Dictionary<string, bool> tempTypeList = new Dictionary<string, bool>();
            string tempTypeName;
            string tempNamespace;
            string[] words;
            foreach (string defaultTypeName in PPLocalStorageManager.Defaults)
            {
                words = defaultTypeName.Split('.');
                if (words.Length == 1)
                {
                    tempNamespace = "";
                    tempTypeName = defaultTypeName;
                }
                else
                {
                    tempNamespace = words[0];
                    tempTypeName = words[1];
                }


                defaultTypes.Add(new PPAutoPersistObject(tempNamespace, tempTypeName, true));
                tempTypeList.Add(tempTypeName, true);
            }

            foreach (System.Type type in asm.GetTypes())
            {
                if (typeof(Component).IsAssignableFrom(type))
                {
                    if (type != typeof(Component) && type != typeof(Behaviour) && type != typeof(MonoBehaviour))
                    {
                        if (!tempTypeList.ContainsKey(type.Name))
                        {
                            defaultTypes.Add(new PPAutoPersistObject("UnityEngine", type.Name, false));
                            tempTypeList.Add(type.Name, false);
                        }
                    }
                }
            }
            
            /*
            asm = typeof(PPAssemblyLocator).Assembly;
            foreach (System.Type type in asm.GetTypes())
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    if (!tempTypeList.ContainsKey(type.Name))
                    {
                        defaultTypes.Add(new PPAutoPersistObject("", type.Name, false));
                        tempTypeList.Add(type.Name, false);
                    }
                }
            }

            asm = typeof(PPAssemblyLocatorJS).Assembly;
            foreach (System.Type type in asm.GetTypes())
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    if (!tempTypeList.ContainsKey(type.Name))
                    {
                        defaultTypes.Add(new PPAutoPersistObject("", type.Name, false));
                        tempTypeList.Add(type.Name, false);
                    }
                }
            }
             * */

#if UNITY_2_6 || UNITY_2_6_1 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4 || UNITY_3_5 || UNITY_3_6 || UNITY_3_7 || UNITY_3_8 || UNITY_3_9 || UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_4_9

           string[] asmNames = UnityEditor.AssemblyHelper.GetNamesOfAssembliesLoadedInCurrentDomain();
            Assembly unityScriptAsm=null;
            foreach (string asmPath in asmNames)
            {
                if (asmPath.EndsWith("Assembly-UnityScript.dll") || asmPath.EndsWith("Assembly-CSharp.dll"))
                {
                    unityScriptAsm=Assembly.LoadFile(asmPath);
                    foreach (System.Type type in unityScriptAsm.GetTypes())
                    {
                        if (typeof(MonoBehaviour).IsAssignableFrom(type))
                        {
                            if (!tempTypeList.ContainsKey(type.Name))
                            {
                                defaultTypes.Add(new PPAutoPersistObject("", type.Name, false));
                                tempTypeList.Add(type.Name, false);
                            }
                        }
                    }
                }
            }
#else //UNITY 5 and up
            Assembly[] currDomainAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            string asmPath;

            foreach (Assembly currDomainAsm in currDomainAssemblies)
            {
                asmPath = currDomainAsm.CodeBase;
                if (asmPath.EndsWith("Assembly-UnityScript.dll") || asmPath.EndsWith("Assembly-CSharp.dll"))
                {
                    foreach (System.Type type in currDomainAsm.GetTypes())
                    {
                        if (typeof(MonoBehaviour).IsAssignableFrom(type))
                        {
                            if (!tempTypeList.ContainsKey(type.Name))
                            {
                                defaultTypes.Add(new PPAutoPersistObject("", type.Name, false));
                                tempTypeList.Add(type.Name, false);
                            }
                        }
                    }
                }
            }
#endif
             
             
            

            SortList();
        }
   
    }

    private static void SortList()
    {
        AutoPersistComparer ac = new AutoPersistComparer();

        defaultTypes.Sort(ac);
    }

    public void OnGUI()
    {
        GUILayout.Label("PlayModePersist - Auto Persist", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        autoPersistSearch = EditorGUILayout.TextField(new GUIContent("Filter"), autoPersistSearch, GUILayout.Width(300.0f));
        EditorGUILayout.Space();
      
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos,false,false, GUILayout.Width(300), GUILayout.Height(this.position.height-140));
        int listCount = 0;

        foreach (PPAutoPersistObject autoPersistObj in defaultTypes)
        {
            if (autoPersistSearch == "" || autoPersistObj.TypeName.ToLower().StartsWith(autoPersistSearch.ToLower()))
            {
                if (!isShowOnlyEnabled || autoPersistObj.IsDefaulted)
                {
                    GUILayout.BeginHorizontal(GUILayout.Width(250));
                    GUILayout.Label(autoPersistObj.TypeName, GUILayout.Width(200));

                    if (!autoPersistObj.IsDefaulted)
                    {
                        if (GUILayout.Button("Add"))
                        {
                            PPLocalStorageManager.AddDefault(autoPersistObj.GetFullClassName());
                            autoPersistObj.IsDefaulted = true;
                            if (EditorApplication.isPlaying || EditorApplication.isPaused)
                            {
                                if (PPEditorWindow.currentWindow!=null)
                                {
                                    PPEditorWindow.SetPersistanceForAllExistingContextObjects(autoPersistObj.TypeName, true);
                                    PPEditorWindow.currentWindow.Repaint();
                                }
                            }
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Remove"))
                        {
                            PPLocalStorageManager.RemoveDefault(autoPersistObj.GetFullClassName());
                            autoPersistObj.IsDefaulted = false;
                            if (EditorApplication.isPlaying || EditorApplication.isPaused)
                            {
                                if (PPEditorWindow.currentWindow != null)
                                {
                                    PPEditorWindow.SetPersistanceForAllExistingContextObjects(autoPersistObj.TypeName,false);
                                    PPEditorWindow.currentWindow.Repaint();
                                }
                            }
                            //add logic to deselect all active object of this type if in PlayMode
                        }

                    }

                    GUILayout.EndHorizontal();
                    listCount++;
                }
            }
        }
        if (listCount == 0)
        {
            if (isShowOnlyEnabled && autoPersistSearch == "")
            {
                GUILayout.Label("No Components Are Auto Persisting");
            }
            else
            {
                GUILayout.Label("Please Broaden Your Search");
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Show Only Enabled");
        isShowOnlyEnabled = EditorGUILayout.Toggle(isShowOnlyEnabled, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
      

        if (SHOW_ADDED_ALL)
        {  
            EditorGUILayout.Space();
            if (GUILayout.Button("Add All"))
            {
                bool dialogContinue = EditorUtility.DisplayDialog("Auto Persist All Components", "Warning: This will persist all changes and may not achieve your required result. Any scripts you add in the future will not be included in this list. In larger project this will have significant editor performance overhead. Do you wish to continue?", "Ok", "Cancel");
                if (dialogContinue)
                {
                    foreach (PPAutoPersistObject autoPersistObj in defaultTypes)
                    {
                        if (!autoPersistObj.IsDefaulted)
                        {
                            PPLocalStorageManager.AddDefault(autoPersistObj.GetFullClassName());
                            autoPersistObj.IsDefaulted = true;
                        }
                    }
                }
            }
        }
        if (PPLocalStorageManager.Defaults.Count>0)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Remove All"))
            {
                foreach (PPAutoPersistObject autoPersistObj in defaultTypes)
                {
                    if (autoPersistObj.IsDefaulted)
                    {
                        PPLocalStorageManager.RemoveDefault(autoPersistObj.GetFullClassName());
                        autoPersistObj.IsDefaulted = false;
                    }
                }
            }
        }
        EditorGUILayout.Space();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();


      //  GUILayout.Label("Note:You will need to have selected a GameObject\nwhile in Playmode for components to auto persist");

        //Experimental Start
        /*
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add All"))
        {
            foreach (PPAutoPersistObject autoPersistObj in defaultTypes)
            {
                PPLocalStorageManager.AddDefault(autoPersistObj.GetFullClassName());
                autoPersistObj.IsDefaulted = true;
                if (EditorApplication.isPlaying || EditorApplication.isPaused)
                {
                    PPInspector.SetPersistanceForAllExistingContextObjects(autoPersistObj.TypeName, true);
                }
            }
        }
        
        if (GUILayout.Button("Remove All"))
        {
            foreach (PPAutoPersistObject autoPersistObj in defaultTypes)
            {
                PPLocalStorageManager.RemoveDefault(autoPersistObj.GetFullClassName());
                autoPersistObj.IsDefaulted = false;
                if (EditorApplication.isPlaying || EditorApplication.isPaused)
                {
                    PPInspector.SetPersistanceForAllExistingContextObjects(autoPersistObj.TypeName, false);
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        */
        //Experimental End
    }
}

public class PPAutoPersistObject
{
    private string typeName;
    private string namespaceStr;
    private bool isDefaulted;

    public PPAutoPersistObject(string namespaceStr,string typeName,bool isDefaulted)
    {
        this.typeName = typeName;
        this.namespaceStr = namespaceStr;
        this.isDefaulted = isDefaulted;
    }
    public string TypeName
    {
        get { return typeName; }
    }

    //including namespace
    public string GetFullClassName()
    {
        if (namespaceStr.Length > 0) return namespaceStr + "." + typeName;
        else return typeName;
    }

    public bool IsDefaulted
    {
        get { return isDefaulted; }
        set { isDefaulted = value; }
    }


}

public class AutoPersistComparer : IComparer<PPAutoPersistObject>
{
    public int Compare(PPAutoPersistObject x, PPAutoPersistObject y)
    {
        if (x == null)
        {
            if (y == null) return 0;
            else return -1;
        }
        else
        {
            if (y == null)
            {
                return 1;
            }
            else
            {
                 return x.TypeName.CompareTo(y.TypeName);
            }
        }
    }
}