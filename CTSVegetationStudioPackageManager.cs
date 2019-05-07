using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System;
using System.IO;
using System.Linq;

using AwesomeTechnologies.ColliderSystem;
using AwesomeTechnologies.PrefabSpawner;
using AwesomeTechnologies.TerrainSystem;
using AwesomeTechnologies.TouchReact;
using AwesomeTechnologies.Vegetation.PersistentStorage;
using AwesomeTechnologies.VegetationStudio;
using AwesomeTechnologies.VegetationSystem;
using CTS;
public enum TerrainTextureCount {
    None,Four,Eight,Twelve,Sixteen
}

public class CTSVegetationStudioPackageManager : EditorWindow
{
    [Serializable] public struct VegetationPackageInfos{
        [SerializeField] public string PopupName;
        [SerializeField] public string _VegetationPackage;
        [SerializeField] public string _CTSProfile;
        [SerializeField] public string[] _TerrainLayers;
        [SerializeField] public int TerrainLayersCount;
    }

    private Vector2 scrollPos;
    private string pathMain,pathLayer;
    private string VegetationPackageName = "New Vegetation Package Pro";
    private VegetationPackagePro VegetationPackage;
    private CTSProfile CTSProfilePackage;
    private TerrainLayer[] TerrainLayers = new TerrainLayer[0];
    private bool foldoutLayers,foldoutTextures,IsRefreshing = false;
    private TerrainTextureCount VegetationPackageTextureCount = TerrainTextureCount.Sixteen;
    private VegetationSystemPro VSP;
    [SerializeField] private VegetationPackageInfos[] VPI = new VegetationPackageInfos[0];
    [SerializeField] private string[] VPINames = new string[0];
    [SerializeField] private int VPIselection;
    private bool disableGroup0;
    [MenuItem("Window/Procedural Worlds/CTS/Vegetation Studio Package Manager &v")]
    static void Init()
    {
       var window = EditorWindow.GetWindow(typeof(CTSVegetationStudioPackageManager));
       window.Show();
    }
    void Awake(){
        GetPath();
        if(EditorPrefs.HasKey("CTSVegetationStudioPackageManager")){
            JsonUtility.FromJsonOverwrite(EditorPrefs.GetString("CTSVegetationStudioPackageManager"),this);
        }
    }
    void OnDestroy(){
        var data = JsonUtility.ToJson(this);
        EditorPrefs.SetString("CTSVegetationStudioPackageManager",data);
    }
   [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        EditorPrefs.DeleteKey("CTSVegetationStudioPackageManager");
    }

    void OnGUI()
    {
        var rect = new Rect(0,0,position.width,22);
        var boxGS = new GUIStyle();
        boxGS.fontSize = 14;
        boxGS.fontStyle = FontStyle.Bold;
        EditorGUI.DrawRect(rect,Color.green/2);
        GUILayout.Space(3);
        EditorGUILayout.LabelField("CTS / Vegetation Studio Pro Package Manager",boxGS);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if(!IsRefreshing){
            EditorGUILayout.BeginVertical("Box");
            VegetationPackageName = EditorGUILayout.TextField("Vegetation Package Name:",VegetationPackageName);
            
            EditorGUILayout.BeginHorizontal();
            VegetationPackageTextureCount = (TerrainTextureCount)EditorGUILayout.EnumPopup("Texture Count",VegetationPackageTextureCount);
           
            if(!VPINames.Contains(VegetationPackageName)){
                if(GUILayout.Button("Create new Package")){
                        VPI = VPI.Concat(new VegetationPackageInfos[]{new VegetationPackageInfos()}).ToArray();
                        AssetDatabase.StartAssetEditing();
                        CreateVegetationPackage();
                        CreateCTSProfile();
                        CreateTerrainLayers();
                        AssetDatabase.StopAssetEditing();
                }
            }else{
                EditorGUILayout.LabelField("This Name Allready Exists! Choose Other");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            if(VPI.Length > 0){
                EditorGUILayout.BeginVertical("Box");
                VPIselection = EditorGUILayout.Popup("Select Package to Load",VPIselection,VPINames);
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Load Package",GUILayout.Width(position.width/2))){
                    LoadAssetToManager(VPINames[VPIselection]);
                }
                if(GUILayout.Button("Delete Package",GUILayout.Width(position.width/2))){
                    DeleteAssetFromManager(VPINames[VPIselection]);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }
            if(CTSProfilePackage != null){

                EditorGUILayout.BeginVertical("Box");
                VegetationPackage = (VegetationPackagePro)EditorGUILayout.ObjectField("Vegetation Package Pro",VegetationPackage,typeof(VegetationPackagePro),false);
                CTSProfilePackage = (CTSProfile)EditorGUILayout.ObjectField("CTS Profile",CTSProfilePackage,typeof(CTSProfile),false);
                

                EditorGUILayout.BeginHorizontal();
                if(TerrainLayers != null && VegetationPackage != null && VSP != null){
                    if(GUILayout.Button("Refresh Packages",GUILayout.Width(position.width/2))){
                        RefreshPackages();
                    }
                }else{
                    EditorGUILayout.LabelField("No Vegetation Studio In Scene");
                }
                if(GUILayout.Button("Remove All",GUILayout.Width(position.width/2))){
                    RemoveAll();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical("Box");
                foldoutLayers = EditorGUILayout.Foldout(foldoutLayers,"Terrain Layers");
                if(foldoutLayers){
                    for(int i = 0; i<TerrainLayers.Length;i++){
                        TerrainLayers[i] = (TerrainLayer)EditorGUILayout.ObjectField(TerrainLayers[i],typeof(TerrainLayer),false);
                    }
                }
                foldoutTextures = EditorGUILayout.Foldout(foldoutTextures,"Terrain Textures");
                if(foldoutTextures){
                    var gs = new GUIStyle();
                    gs.fontStyle = FontStyle.Bold;
                    gs.fontSize = 12;
                    for(int i = 0; i<CTSProfilePackage.TerrainTextures.Count;i++){
                        GUILayout.Label(CTSProfilePackage.TerrainTextures[i].Albedo.name+"_" +i+ ":",gs);
                        CTSProfilePackage.TerrainTextures[i].Albedo = (Texture2D)EditorGUILayout.ObjectField(CTSProfilePackage.TerrainTextures[i].Albedo,typeof(Texture2D),false);
                        CTSProfilePackage.TerrainTextures[i].Normal = (Texture2D)EditorGUILayout.ObjectField(CTSProfilePackage.TerrainTextures[i].Normal,typeof(Texture2D),false);
                        CTSProfilePackage.TerrainTextures[i].Smoothness = (Texture2D)EditorGUILayout.ObjectField(CTSProfilePackage.TerrainTextures[i].Smoothness,typeof(Texture2D),false);
                        CTSProfilePackage.TerrainTextures[i].Roughness = (Texture2D)EditorGUILayout.ObjectField(CTSProfilePackage.TerrainTextures[i].Roughness,typeof(Texture2D),false);
                        CTSProfilePackage.TerrainTextures[i].Height = (Texture2D)EditorGUILayout.ObjectField(CTSProfilePackage.TerrainTextures[i].Height,typeof(Texture2D),false);
                        GUILayout.HorizontalSlider(0,0,0);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
        if(VegetationPackage != null && CTSProfilePackage != null){
            if(GUILayout.Button("Setup this Package")){
                if(!FindObjectOfType<VegetationStudioManager>() && !CTSTerrainManager.Instance.ProfileIsActive(CTSProfilePackage)){
                    ConnectToCTI_VSP(CTSProfilePackage,VegetationPackage);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
    void CreateVegetationPackage(){
        var v = new VegetationPackagePro();
        v.name = VegetationPackageName;
        switch(VegetationPackageTextureCount){
            case TerrainTextureCount.None:
                v.TerrainTextureCount = 0;
            break;
            case TerrainTextureCount.Four:
                v.TerrainTextureCount = 4;
            break;
            case TerrainTextureCount.Eight:
                v.TerrainTextureCount = 8;
            break;
            case TerrainTextureCount.Twelve:
                v.TerrainTextureCount = 12;
            break;
            case TerrainTextureCount.Sixteen:
                v.TerrainTextureCount = 16;
            break;
        }

        v.InitPackage();
        v.LoadDefaultTextures();
        v.SetupTerrainTextureSettings();
        AssetDatabase.CreateAsset(v,pathMain+"/"+v.name+".asset");
        VPI[VPI.Length-1].PopupName = v.name;
        VPINames = VPINames.Concat(new string[]{v.name}).ToArray();
        VPI[VPI.Length-1]._VegetationPackage = (pathMain+"/"+v.name+".asset");
        VegetationPackage = v;
    }
    void CreateCTSProfile(){
        var CTSpro = new CTSProfile();
        CTSpro.name = VegetationPackage.name+" CTSProfile";
        CTSpro.TerrainTextures = new List<CTS.CTSTerrainTextureDetails>();
        for(int i = 0; i<VegetationPackage.TerrainTextureCount;i++){
            var ctsTex = new CTS.CTSTerrainTextureDetails();
            ctsTex.Albedo = VegetationPackage.TerrainTextureList[i].Texture;
            ctsTex.Normal = VegetationPackage.TerrainTextureList[i].TextureNormals;
            CTSpro.TerrainTextures.Add(ctsTex);
        }
        AssetDatabase.CreateAsset(CTSpro,pathMain+"/"+CTSpro.name+".asset");
        VPI[VPI.Length-1]._CTSProfile = pathMain+"/"+CTSpro.name+".asset";
        CTSProfilePackage = CTSpro;
    }
    void CreateTerrainLayers(){
        TerrainLayers = new TerrainLayer[CTSProfilePackage.TerrainTextures.Count];
        VPI[VPI.Length-1]._TerrainLayers = new string[TerrainLayers.Length];
        VPI[VPI.Length-1].TerrainLayersCount = TerrainLayers.Length;
        for(int i = 0; i<TerrainLayers.Length;i++){
            var layer = new TerrainLayer();
            layer.diffuseTexture = CTSProfilePackage.TerrainTextures[i].Albedo;
            layer.normalMapTexture = CTSProfilePackage.TerrainTextures[i].Normal;
            TerrainLayers[i] = layer;
            AssetDatabase.CreateAsset(TerrainLayers[i], pathLayer+"/"+VegetationPackage.name+" Terrain Layer "+ i+".terrainlayer");
            VPI[VPI.Length-1]._TerrainLayers[i] =       (pathLayer+"/"+VegetationPackage.name+" Terrain Layer "+ i+".terrainlayer");
        }
    }
    void RefreshPackages(){
        for(int i = 0; i<CTSProfilePackage.TerrainTextures.Count;i++){
            TerrainLayers[i].diffuseTexture = CTSProfilePackage.TerrainTextures[i].Albedo;
            TerrainLayers[i].normalMapTexture = CTSProfilePackage.TerrainTextures[i].Normal;

            VegetationPackage.TerrainTextureList[i].Texture =  CTSProfilePackage.TerrainTextures[i].Albedo;
            VegetationPackage.TerrainTextureList[i].TextureNormals =  CTSProfilePackage.TerrainTextures[i].Normal;
        }
        AssetDatabase.Refresh();
        CTSTerrainManager.Instance.BroadcastProfileUpdate(CTSProfilePackage);
        VSP.VegetationPackageProList[0] = VegetationPackage;
    }
    void GetPath(){
        if(AssetDatabase.IsValidFolder("Assets/Procedural Worlds/CTS")&& !AssetDatabase.IsValidFolder("Assets/Procedural Worlds/CTS/Vegetation Studio Addon")){
            AssetDatabase.CreateFolder("Assets/Procedural Worlds/CTS","Vegetation Studio Addon");
            AssetDatabase.CreateFolder("Assets/Procedural Worlds/CTS/Vegetation Studio Addon","Terrain Layers");
        }
        if(!AssetDatabase.IsValidFolder("Assets/Procedural Worlds/CTS") && !AssetDatabase.IsValidFolder("Assets/Vegetation Studio Addon"))
        {
            AssetDatabase.CreateFolder("Assets","Vegetation Studio Addon");
            AssetDatabase.CreateFolder("Assets/Vegetation Studio Addon","Terrain Layers");
        }
        if(AssetDatabase.IsValidFolder("Assets/Vegetation Studio Addon")){
            pathMain =     "Assets/Vegetation Studio Addon";
            pathLayer =    "Assets/Vegetation Studio Addon/Terrain Layers";
        }
        if(AssetDatabase.IsValidFolder("Assets/Procedural Worlds/CTS/Vegetation Studio Addon")){
            pathMain =     "Assets/Procedural Worlds/CTS/Vegetation Studio Addon";
            pathLayer =    "Assets/Procedural Worlds/CTS/Vegetation Studio Addon/Terrain Layers";
        }
    }
    void RemoveAll(){
        IsRefreshing = true;
        AssetDatabase.DeleteAsset(pathMain);
        VPI = new VegetationPackageInfos[0];
        VPINames = new string[0];
        AssetDatabase.Refresh();
        GetPath();
        IsRefreshing = false;
    }
    void LoadAssetToManager(string AssetName){
        TerrainLayers = new TerrainLayer[0];
        AssetDatabase.StartAssetEditing();
            for(int i = 0; i<VPI.Length;i++){
                if(VPI[i].PopupName == AssetName){
                    VegetationPackage = (VegetationPackagePro)AssetDatabase.LoadAssetAtPath(VPI[i]._VegetationPackage,typeof(VegetationPackagePro));
                    CTSProfilePackage = (CTSProfile)AssetDatabase.LoadAssetAtPath(VPI[i]._CTSProfile,typeof(CTSProfile));
                    
                    for(int index = 0;index<VPI[i].TerrainLayersCount;index++){
                        TerrainLayers = TerrainLayers.Concat(new TerrainLayer[]{
                            (TerrainLayer)AssetDatabase.LoadAssetAtPath(VPI[i]._TerrainLayers[index],typeof(TerrainLayer))
                        }).ToArray(); 
                    }
                    
                }
            }
        AssetDatabase.StopAssetEditing();
        }
    void DeleteAssetFromManager(string AssetName){
        AssetDatabase.StartAssetEditing();
        int index = 0;
        for(int i = 0; i<VPI.Length;i++){
            if(VPI[i].PopupName == AssetName){
                index = i;
                AssetDatabase.DeleteAsset(VPI[i]._VegetationPackage);
                AssetDatabase.DeleteAsset(VPI[i]._CTSProfile);
                for(int l = 0; l<VPI[i].TerrainLayersCount;l++){
                    AssetDatabase.DeleteAsset(VPI[i]._TerrainLayers[l]);
                }
            }
            var v = VPI.ToList();
            var vn = VPINames.ToList();
            
            v.RemoveAt(index);
            v.Sort();

            vn.RemoveAt(index);
            vn.Sort();

            VPINames = vn.ToArray();
            VPI = v.ToArray();
        }
        VPIselection = VPI.Length-1;
        AssetDatabase.StopAssetEditing();
    }
    void ConnectToCTI_VSP(CTSProfile profile, VegetationPackagePro PackagePro){

        
        CTSTerrainManager.Instance.AddCTSToAllTerrains();
        CTSTerrainManager.Instance.BroadcastProfileSelect(profile);

        VegetationStudioManager vegetationStudioManager = FindObjectOfType<VegetationStudioManager>();
        if (!vegetationStudioManager)
        {
            GameObject go = new GameObject {name = "VegetationStudioPro"};
            go.AddComponent<VegetationStudioManager>();

            GameObject vegetationSystem = new GameObject {name = "VegetationSystemPro"};
            vegetationSystem.transform.SetParent(go.transform);
            VSP = vegetationSystem.AddComponent<VegetationSystemPro>();
            vegetationSystem.AddComponent<TerrainSystemPro>();
            VSP.AddAllUnityTerrains();
            VSP.AddVegetationPackage(PackagePro);
            PackagePro.SetupTerrainTextureSettings();
            
        #if TOUCH_REACT
            GameObject touchReactSystem = new GameObject { name = "TouchReactSystem" };
            touchReactSystem.transform.SetParent(go.transform);               
            touchReactSystem.AddComponent<TouchReactSystem>();
        #endif                
            vegetationSystem.AddComponent<ColliderSystemPro>();
            vegetationSystem.AddComponent<PersistentVegetationStorage>();
            RuntimePrefabSpawner runtimePrefabSpawner =  vegetationSystem.AddComponent<RuntimePrefabSpawner>();
            runtimePrefabSpawner.enabled = false;
        }
    }
}
    