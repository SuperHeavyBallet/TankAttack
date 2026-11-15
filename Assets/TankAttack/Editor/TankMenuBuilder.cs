using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace TankAttack.Editor
{
    public static class TankMenuBuilder
    {
        const string Root=PrototypeBuilder.Root;
        public const string MenuPath=Root+"/Scenes/TankAttack_Menu.unity";
        static Font font;
        [MenuItem("Tank Attack/Install Tank Selection and Engine Audio")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play mode before installing menu content.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(PrototypeBuilder.ScenePath);
            var session=UnityEngine.Object.FindAnyObjectByType<BattleSession>();
            if(session==null || session.player==null) throw new InvalidOperationException("Prototype requires its existing player tank.");
            var input=session.player.GetComponent<PlayerTankInput>();
            var audio=session.player.GetComponent<VehicleAudio>();
            var engine=Create<EngineAudioSettings>("PlayerEngineAudio",a=> { a.rumbleClip=audio.engineLoop; a.volume=audio.engineVolume; a.mixerGroup=audio.mixerGroup; });
            var mediumVehicle=session.player.settings; var mediumWeapon=input.cannon.settings;
            var lightVehicle=Clone(mediumVehicle,"LightPlayerVehicle",v=>
            { v.health*=.7f; v.forwardSpeed*=1.35f; v.reverseSpeed*=1.35f; v.hullTurnSpeed*=1.5f; v.acceleration*=1.45f; v.braking*=1.45f; v.turretTurnSpeed*=1.2f; });
            var heavyVehicle=Clone(mediumVehicle,"HeavyPlayerVehicle",v=>
            { v.health*=1.8f; v.forwardSpeed*=.65f; v.reverseSpeed*=.65f; v.hullTurnSpeed*=.65f; v.acceleration*=.65f; v.braking*=.75f; v.turretTurnSpeed*=.75f; });
            var lightWeapon=Clone(mediumWeapon,"LightPlayerCannon",w=> { w.displayName="Light Cannon"; w.damage*=.65f; w.interval*=.85f; });
            var heavyWeapon=Clone(mediumWeapon,"HeavyPlayerCannon",w=> { w.displayName="Heavy Base Cannon"; w.damage*=1.8f; w.interval*=1.3f; });
            var light=Definition("LightPlayerTank","TANK 1 / SCOUT","Light armour. High speed and tight cornering.",lightVehicle,lightWeapon,engine,new Color(.25f,.8f,.95f));
            var medium=Definition("MediumPlayerTank","TANK 2 / VANGUARD","Balanced armour, handling and firepower.",mediumVehicle,mediumWeapon,engine,new Color(.5f,.9f,.65f));
            var heavy=Definition("HeavyPlayerTank","TANK 3 / BASTION","Heavy armour and a powerful cannon. Slower to manoeuvre.",heavyVehicle,heavyWeapon,engine,new Color(1,.62f,.3f));
            var roster=Create<TankRoster>("PlayerTankRoster",r=>r.tanks=new[]{light,medium,heavy});
            var original=PrefabUtility.GetCorrespondingObjectFromSource(session.player.gameObject);
            if(original!=null)
            {
                string prefabPath=AssetDatabase.GetAssetPath(original); var contents=PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var config=contents.GetComponent<PlayerTankConfiguration>(); if(config==null) config=contents.AddComponent<PlayerTankConfiguration>();
                    if(config.defaultTank==null) config.defaultTank=medium;
                    var vehicleAudio=contents.GetComponent<VehicleAudio>(); if(vehicleAudio.engineSettings==null) vehicleAudio.engineSettings=engine;
                    PrefabUtility.SaveAsPrefabAsset(contents,prefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            var sceneConfig=session.player.GetComponent<PlayerTankConfiguration>();
            if(sceneConfig==null) sceneConfig=session.player.gameObject.AddComponent<PlayerTankConfiguration>();
            if(sceneConfig.defaultTank==null) sceneConfig.defaultTank=medium;
            if(audio.engineSettings==null) audio.engineSettings=engine;
            if(PrefabUtility.IsPartOfPrefabInstance(session.player))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(sceneConfig);
                PrefabUtility.RecordPrefabInstancePropertyModifications(audio);
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            if(!File.Exists(MenuPath)) BuildMenu(roster);
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(MenuPath,true),new EditorBuildSettingsScene(PrototypeBuilder.ScenePath,true)};
            foreach(var scene in EditorBuildSettings.scenes) if(scene.path!=MenuPath && scene.path!=PrototypeBuilder.ScenePath) scenes.Add(scene);
            EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(MenuPath); Debug.Log("TANK_ATTACK_MENU_INSTALLED");
        }
        [MenuItem("Tank Attack/Open Main Menu")]
        public static void OpenMenu()
        {
            if(!File.Exists(MenuPath)) { Apply(); return; }
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MenuPath);
        }
        static T Create<T>(string name,Action<T> init) where T:ScriptableObject
        {
            string path=Root+"/Settings/"+name+".asset"; var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset!=null) return asset;
            asset=ScriptableObject.CreateInstance<T>(); init(asset); AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static T Clone<T>(T template,string name,Action<T> init) where T:ScriptableObject
        {
            string path=Root+"/Settings/"+name+".asset"; var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset!=null) return asset;
            asset=UnityEngine.Object.Instantiate(template); asset.name=name; init(asset); AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static TankDefinition Definition(string name,string title,string description,VehicleSettings vehicle,WeaponSettings cannon,EngineAudioSettings engine,Color accent)
        {
            return Create<TankDefinition>(name,d=> { d.displayName=title; d.description=description; d.vehicle=vehicle; d.cannon=cannon; d.engineAudio=engine; d.accent=accent; });
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1); rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height); return rect;
        }
        static Image Panel(string name,Transform parent,float x,float y,float width,float height,Color color)
        { var image=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Image>(); image.color=color; return image; }
        static Text Label(string name,Transform parent,float x,float y,float width,float height,string value,int size,Color color,TextAnchor alignment=TextAnchor.UpperLeft)
        {
            var text=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Text>(); text.font=font; text.text=value;
            text.fontSize=size; text.color=color; text.alignment=alignment; text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate; text.lineSpacing=1.15f; return text;
        }
        static void TankIcon(Transform parent,Color accent,int index)
        {
            float width=46+index*8;
            Panel("Left track",parent,355,151,14,72,new Color(.025f,.035f,.05f)).raycastTarget=false;
            Panel("Right track",parent,355+width+12,151,14,72,new Color(.025f,.035f,.05f)).raycastTarget=false;
            Panel("Hull",parent,369,157,width-2,60,accent*.75f).raycastTarget=false;
            Panel("Turret",parent,375,166,width-14,30,accent).raycastTarget=false;
            Panel("Barrel",parent,369+width/2-4,137,7+index*2,40,accent).raycastTarget=false;
        }
        static void BuildMenu(TankRoster roster)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var cam=new GameObject("Menu Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.transform.position=new Vector3(0,0,-10);
            cam.orthographic=true; cam.orthographicSize=5; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.035f,.05f,.075f); cam.gameObject.AddComponent<AudioListener>();
            var canvas=new GameObject("Tank Selection Canvas",typeof(RectTransform)).AddComponent<Canvas>();
            canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=cam; canvas.planeDistance=1;
            var scaler=canvas.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1600,900); scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight=.5f;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            var background=Panel("Background",canvas.transform,0,0,1600,900,new Color(.035f,.05f,.075f));
            background.rectTransform.anchorMin=Vector2.zero; background.rectTransform.anchorMax=Vector2.one; background.rectTransform.offsetMin=background.rectTransform.offsetMax=Vector2.zero;
            Label("Eyebrow",canvas.transform,64,30,1000,28,"01 / VEHICLE SELECTION",16,new Color(.4f,.7f,.8f));
            Label("Title",canvas.transform,60,63,1200,85,"TANK ATTACK",66,Color.white).fontStyle=FontStyle.Bold;
            Label("Subtitle",canvas.transform,64,153,1400,32,"Choose your armour. Make it to extraction.",22,new Color(.65f,.72f,.8f));
            var menu=canvas.gameObject.AddComponent<TankMenuController>(); menu.roster=roster;
            menu.tankButtons=new Button[3]; menu.cardBackgrounds=new Image[3]; menu.selectionBars=new Image[3];
            menu.cardNames=new Text[3]; menu.cardRoles=new Text[3]; menu.cardStats=new Text[3];
            for(int i=0;i<3;i++)
            {
                var panel=Panel("Tank "+(i+1),canvas.transform,64+i*496,206,480,244,new Color(.075f,.10f,.14f));
                var button=panel.gameObject.AddComponent<Button>(); button.targetGraphic=panel;
                var colors=button.colors; colors.highlightedColor=new Color(1.15f,1.15f,1.15f); colors.pressedColor=new Color(.85f,.85f,.85f); colors.selectedColor=Color.white; button.colors=colors;
                menu.tankButtons[i]=button; menu.cardBackgrounds[i]=panel;
                menu.selectionBars[i]=Panel("Selection bar",panel.transform,0,0,480,5,roster.tanks[i].accent);
                menu.cardNames[i]=Label("Name",panel.transform,24,24,432,40,roster.tanks[i].displayName,27,Color.white); menu.cardNames[i].fontStyle=FontStyle.Bold;
                menu.cardRoles[i]=Label("Role",panel.transform,24,80,420,65,roster.tanks[i].description,19,new Color(.7f,.76f,.82f));
                menu.cardStats[i]=Label("Summary",panel.transform,24,167,320,65,"",21,roster.tanks[i].accent);
                TankIcon(panel.transform,roster.tanks[i].accent,i);
                var focus=panel.gameObject.AddComponent<TankCardFocus>(); focus.menu=menu; focus.index=i;
            }
            var detail=Panel("Selected vehicle",canvas.transform,64,474,1472,298,new Color(.055f,.075f,.105f));
            menu.selectedName=Label("Selected name",detail.transform,24,24,520,45,"",31,Color.white);
            menu.selectedDescription=Label("Selected description",detail.transform,24,89,490,110,"",23,new Color(.7f,.76f,.82f));
            Label("Armour note",detail.transform,24,230,510,42,"Armour is represented by maximum hull health.",16,new Color(.5f,.6f,.7f));
            menu.statLabels=Label("Stat labels",detail.transform,590,24,370,256,"",20,new Color(.55f,.65f,.75f));
            menu.statValues=Label("Stat values",detail.transform,1010,24,430,256,"",20,Color.white,TextAnchor.UpperRight);
            menu.status=Label("Status",canvas.transform,64,802,1050,60,"Select your tank, then confirm deployment.",20,new Color(.7f,.76f,.82f));
            var confirm=Panel("Confirm",canvas.transform,1176,800,360,66,new Color(.25f,.65f,.5f));
            menu.confirmButton=confirm.gameObject.AddComponent<Button>(); menu.confirmButton.targetGraphic=confirm;
            menu.confirmLabel=Label("Confirm label",confirm.transform,0,0,360,66,"CONFIRM & DEPLOY",24,Color.white,TextAnchor.MiddleCenter); menu.confirmLabel.fontStyle=FontStyle.Bold;
            for(int i=0;i<3;i++) menu.tankButtons[i].navigation=new Navigation { mode=Navigation.Mode.Explicit,selectOnLeft=menu.tankButtons[(i+2)%3],selectOnRight=menu.tankButtons[(i+1)%3],selectOnDown=menu.confirmButton };
            menu.confirmButton.navigation=new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=menu.tankButtons[1]};
            var events=new GameObject("Event System"); events.AddComponent<EventSystem>(); events.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene,MenuPath);
        }
    }
}
