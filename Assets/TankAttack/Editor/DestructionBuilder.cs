using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace TankAttack.Editor
{
    public static class DestructionBuilder
    {
        const string Root="Assets/TankAttack";
        const string EffectPath=Root+"/Prefabs/EnemyExplosion.prefab";
        [MenuItem("Tank Attack/Set Up Enemy Destruction")]
        public static void Apply()
        {
            var effectObject=AssetDatabase.LoadAssetAtPath<GameObject>(EffectPath);
            var explosion=effectObject != null ? effectObject.GetComponent<ParticleSystem>() : null;
            if (explosion == null) explosion=CreateExplosion();
            foreach (string name in new[] {"EnemyTank","LightTank","ScoutCar"})
            {
                string path=Root+"/Prefabs/"+name+".prefab";
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var destruction=prefab.GetComponent<EnemyDestruction>();
                    bool changed=destruction == null;
                    if (destruction == null) destruction=prefab.AddComponent<EnemyDestruction>();
                    if (destruction.explosionPrefab == null) { destruction.explosionPrefab=explosion; changed=true; }
                    if (changed) PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally { PrefabUtility.UnloadPrefabContents(prefab); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("TANK_ATTACK_DESTRUCTION_READY");
        }
        static ParticleSystem CreateExplosion()
        {
            string texturePath=Root+"/Materials/ExplosionSoftParticle.png";
            if (!File.Exists(texturePath))
            {
                const int size=64; var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
                var pixels=new Color[size*size];
                for(int y=0;y<size;y++) for(int x=0;x<size;x++)
                {
                    float radius=new Vector2((x+.5f)/size*2-1,(y+.5f)/size*2-1).magnitude;
                    float alpha=Mathf.Pow(Mathf.Clamp01(1-radius*radius),2);
                    pixels[y*size+x]=new Color(1,1,1,alpha);
                }
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(texturePath,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(texturePath);
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                importer.alphaIsTransparency=true; importer.wrapMode=TextureWrapMode.Clamp; importer.mipmapEnabled=false; importer.SaveAndReimport();
            }
            var textureAsset=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var fireMaterial=Material("ExplosionFire",textureAsset,true);
            var smokeMaterial=Material("ExplosionSmoke",textureAsset,false);
            var root=new GameObject("Enemy Explosion");
            var flash=Particles(root,"Flash",fireMaterial,1,.18f,0,4.5f,new Color(1,.8f,.35f),false);
            root.AddComponent<ExplosionLifetime>();
            var fire=Particles(root,"Fireball",fireMaterial,26,.85f,5,1.6f,new Color(1,.35f,.03f),true);
            var sparks=Particles(root,"Sparks",fireMaterial,35,1.2f,9,.12f,new Color(1,.65f,.15f),true);
            var smoke=Particles(root,"Smoke",smokeMaterial,20,3.5f,1.5f,2.2f,new Color(.18f,.19f,.2f,.7f),true);
            var fireMain=fire.main; fireMain.gravityModifier=.15f;
            var sparksMain=sparks.main; sparksMain.gravityModifier=.8f;
            var smokeMain=smoke.main; smokeMain.gravityModifier=-.05f;
            var sparkRenderer=sparks.GetComponent<ParticleSystemRenderer>();
            sparkRenderer.renderMode=ParticleSystemRenderMode.Stretch; sparkRenderer.lengthScale=2; sparkRenderer.velocityScale=.12f;
            var smokeSize=smoke.sizeOverLifetime; smokeSize.enabled=true;
            smokeSize.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.5f,1,2));
            var fireColor=fire.colorOverLifetime; fireColor.enabled=true;
            fireColor.color=Fade(new Color(1,.9f,.5f),new Color(.55f,.12f,.01f));
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,EffectPath); Object.DestroyImmediate(root);
            return prefab.GetComponent<ParticleSystem>();
        }
        static Material Material(string name,Texture texture,bool additive)
        {
            string path=Root+"/Materials/"+name+".mat";
            var existing=AssetDatabase.LoadAssetAtPath<Material>(path); if(existing != null) return existing;
            var material=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap",texture); material.SetColor("_BaseColor",Color.white);
            material.SetFloat("_Surface",1); material.SetFloat("_Blend",additive ? 2 : 0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend",(float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One); material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0); material.SetFloat("_Cull",(float)CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType","Transparent");
            material.renderQueue=(int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material,path); return material;
        }
        static Gradient Fade(Color start,Color end)
        {
            var gradient=new Gradient();
            gradient.SetKeys(new[] {new GradientColorKey(start,0),new GradientColorKey(end,1)},
                new[] {new GradientAlphaKey(1,0),new GradientAlphaKey(.85f,.2f),new GradientAlphaKey(0,1)});
            return gradient;
        }
        static ParticleSystem Particles(GameObject root,string name,Material material,int count,float lifetime,float speed,float size,Color color,bool child)
        {
            var go=child ? new GameObject(name) : root;
            if(child) go.transform.SetParent(root.transform,false);
            var particles=go.AddComponent<ParticleSystem>(); particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main; main.loop=false; main.playOnAwake=true; main.duration=.2f; main.maxParticles=count;
            main.startLifetime=new ParticleSystem.MinMaxCurve(lifetime*.6f,lifetime);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.5f,speed);
            main.startSize=new ParticleSystem.MinMaxCurve(size*.7f,size*1.3f); main.startColor=color;
            main.startRotation=new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
            main.scalingMode=ParticleSystemScalingMode.Hierarchy; main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=particles.emission; emission.rateOverTime=0;
            emission.SetBursts(new[] {new ParticleSystem.Burst(0,(short)count)});
            var shape=particles.shape; shape.enabled=speed>0; shape.shapeType=ParticleSystemShapeType.Sphere; shape.radius=.4f;
            var fade=particles.colorOverLifetime; fade.enabled=true; fade.color=Fade(Color.white,Color.white);
            var sizeModule=particles.sizeOverLifetime; sizeModule.enabled=true; sizeModule.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.6f,1,1.5f));
            var renderer=particles.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off; renderer.receiveShadows=false;
            return particles;
        }
    }
}
